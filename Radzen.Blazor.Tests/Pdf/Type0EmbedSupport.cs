#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Shared plumbing for the Type0/CID embedding tests. The PINNED internal API is
// Radzen.Documents.Pdf.Fonts.Type0FontEmbedder.Embed(DocumentWriter, SfntFont,
// IReadOnlyDictionary<ushort,int> gidToUnicode) -> ReferenceObject (the indirect
// top-level Type0 font dictionary). Every numeric expectation is derived from the
// exact fixture via SfntFont/CffFont (already-merged, independently tested) and
// cross-checked against fontTools 4.60.2 in the test comments.
internal static class Type0EmbedSupport
{
    public static SfntFont LoadLiberation()
        => SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf"));

    public static SfntFont LoadNoto()
        => SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf"));

    // Distinct code point -> glyph id map for a sample string, dropping uncovered
    // code points. Fixtures used here are entirely BMP so char iteration is safe.
    public static Dictionary<ushort, int> BuildMap(SfntFont font, string text)
    {
        var map = new Dictionary<ushort, int>();
        foreach (var ch in text)
        {
            var gid = font.GetGlyphId(ch);
            Assert.NotEqual(0, gid);
            map[gid] = ch;
        }

        return map;
    }

    public static int ScaleWidth(SfntFont font, int gid)
        => (int)Math.Round(font.GetAdvanceWidth((ushort)gid) * 1000.0 / font.UnitsPerEm, MidpointRounding.AwayFromZero);

    public sealed class Embedded
    {
        public required DocumentReader Reader { get; init; }

        public required DictionaryObject Top { get; init; }

        public required DictionaryObject Descendant { get; init; }

        public required DictionaryObject Descriptor { get; init; }
    }

    public static Embedded Embed(SfntFont font, IReadOnlyDictionary<ushort, int> gidToUnicode)
    {
        using var buffer = new MemoryStream();
        var writer = new DocumentWriter(buffer);
        var topRef = Type0FontEmbedder.Embed(writer, font, gidToUnicode);
        writer.Close();

        var reader = DocumentReader.Parse(buffer.ToArray());
        var top = Dict(reader, reader.GetObject(topRef.ObjectNumber));

        var descendants = (ArrayObject)reader.Resolve(top["DescendantFonts"]);
        Assert.Equal(1, descendants.Count);
        var descendant = Dict(reader, descendants[0]);
        var descriptor = Dict(reader, descendant["FontDescriptor"]);

        return new Embedded
        {
            Reader = reader,
            Top = top,
            Descendant = descendant,
            Descriptor = descriptor,
        };
    }

    public static DictionaryObject Dict(DocumentReader reader, DocumentObject value)
        => (DictionaryObject)reader.Resolve(value);

    public static StreamObject Stream(DocumentReader reader, DocumentObject value)
        => (StreamObject)reader.Resolve(value);

    public static string Name(DocumentReader reader, DictionaryObject dict, string key)
        => ((NameObject)reader.Resolve(dict[key])).Value;

    public static string Str(DocumentReader reader, DictionaryObject dict, string key)
        => ((StringObject)reader.Resolve(dict[key])).Value;

    public static double Num(DocumentReader reader, DictionaryObject dict, string key)
        => ((NumberObject)reader.Resolve(dict[key])).DoubleValue;

    public static byte[] DecodeStream(DocumentReader reader, StreamObject stream)
    {
        if (!stream.Dictionary.TryGetValue("Filter", out var filter) || filter is null)
        {
            return stream.Data;
        }

        var name = ((NameObject)filter).Value;
        Assert.Equal("FlateDecode", name);
        return FlateFilter.Decode(stream.Data);
    }

    // Parse a PDF /W array into CID -> width. Handles both the "c [w ...]" list form
    // and the "cFirst cLast w" range form (ISO 32000-1 9.7.4.3).
    public static Dictionary<int, int> ParseWidths(DocumentReader reader, ArrayObject w)
    {
        var result = new Dictionary<int, int>();
        var i = 0;
        while (i < w.Count)
        {
            var first = (int)((NumberObject)reader.Resolve(w[i])).DoubleValue;
            var next = reader.Resolve(w[i + 1]);
            if (next is ArrayObject list)
            {
                for (var k = 0; k < list.Count; k++)
                {
                    result[first + k] = (int)((NumberObject)reader.Resolve(list[k])).DoubleValue;
                }

                i += 2;
            }
            else
            {
                var last = (int)((NumberObject)next).DoubleValue;
                var width = (int)((NumberObject)reader.Resolve(w[i + 2])).DoubleValue;
                for (var cid = first; cid <= last; cid++)
                {
                    result[cid] = width;
                }

                i += 3;
            }
        }

        return result;
    }

    public static bool CidBit(byte[] cidSet, int cid)
    {
        var byteIndex = cid >> 3;
        if (byteIndex >= cidSet.Length)
        {
            return false;
        }

        var mask = (byte)(0x80 >> (cid & 7));
        return (cidSet[byteIndex] & mask) != 0;
    }

    public static HashSet<int> SetBits(byte[] cidSet)
    {
        var set = new HashSet<int>();
        for (var i = 0; i < cidSet.Length; i++)
        {
            for (var bit = 0; bit < 8; bit++)
            {
                if ((cidSet[i] & (0x80 >> bit)) != 0)
                {
                    set.Add((i * 8) + bit);
                }
            }
        }

        return set;
    }

    // Parse a ToUnicode CMap: CID (2-byte source) -> destination string. Supports
    // the beginbfchar and (triple form) beginbfrange sections.
    public static Dictionary<int, string> ParseToUnicode(byte[] cmap)
    {
        var text = Encoding.Latin1.GetString(cmap);
        var map = new Dictionary<int, string>();

        foreach (Match block in Regex.Matches(text, "beginbfchar(.*?)endbfchar", RegexOptions.Singleline))
        {
            foreach (Match entry in Regex.Matches(block.Groups[1].Value, "<([0-9A-Fa-f]+)>\\s*<([0-9A-Fa-f]+)>"))
            {
                var cid = int.Parse(entry.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                map[cid] = Utf16Be(entry.Groups[2].Value);
            }
        }

        foreach (Match block in Regex.Matches(text, "beginbfrange(.*?)endbfrange", RegexOptions.Singleline))
        {
            foreach (Match entry in Regex.Matches(block.Groups[1].Value, "<([0-9A-Fa-f]+)>\\s*<([0-9A-Fa-f]+)>\\s*<([0-9A-Fa-f]+)>"))
            {
                var lo = int.Parse(entry.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                var hi = int.Parse(entry.Groups[2].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                var dst = int.Parse(entry.Groups[3].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                for (var cid = lo; cid <= hi; cid++)
                {
                    map[cid] = char.ConvertFromUtf32(dst + (cid - lo));
                }
            }
        }

        return map;
    }

    private static string Utf16Be(string hex)
    {
        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = byte.Parse(hex.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return Encoding.BigEndianUnicode.GetString(bytes);
    }

    public static string Reconstruct(SfntFont font, Dictionary<int, string> toUnicode, string text)
    {
        var builder = new StringBuilder();
        foreach (var ch in text)
        {
            var gid = font.GetGlyphId(ch);
            builder.Append(toUnicode[gid]);
        }

        return builder.ToString();
    }
}
