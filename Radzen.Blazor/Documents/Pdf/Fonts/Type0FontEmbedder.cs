using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Pdf.Fonts.Cff;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;

namespace Radzen.Documents.Pdf.Fonts;

// Type0/CID font object graph per ISO 32000-1 9.7.
internal static class Type0FontEmbedder
{
    private const int StemV = 80;
    private const int DefaultWidth = 1000;

    public static ReferenceObject Embed(DocumentWriter writer, SfntFont font, IReadOnlyDictionary<ushort, int> gidToUnicode, IReadOnlyDictionary<ushort, ushort>? compactGidMap = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(gidToUnicode);

        var usedGids = new SortedSet<ushort>(gidToUnicode.Keys);
        var scale = 1000.0 / font.UnitsPerEm;
        var prefix = SubsetTag(usedGids);
        var baseName = prefix + "+" + (string.IsNullOrEmpty(font.PostScriptName) ? "Font" : font.PostScriptName);

        var descriptor = new DictionaryObject
        {
            ["Type"] = new NameObject("FontDescriptor"),
            ["FontName"] = new NameObject(baseName),
            ["Flags"] = new NumberObject(font.Italic ? 96 : 32),
            ["FontBBox"] = FontBBox(font, scale),
            ["ItalicAngle"] = new NumberObject(font.ItalicAngle),
            ["Ascent"] = new NumberObject(Scale(font.Ascent, scale)),
            ["Descent"] = new NumberObject(Scale(font.Descent, scale)),
            ["CapHeight"] = new NumberObject(CapHeight(font, scale)),
            ["StemV"] = new NumberObject(StemV),
        };

        // CIDSet flags exactly the glyphs in the embedded subset (PDF/A 6.2.11.4.2).
        IReadOnlyDictionary<ushort, ushort> gidMap;
        if (font.IsCff)
        {
            gidMap = compactGidMap ?? CffSubsetter.BuildCompactGidMap(usedGids);
            EmbedCff(writer, font, usedGids, descriptor);
        }
        else
        {
            gidMap = compactGidMap ?? GlyfSubsetter.BuildCompactGidMap(font, usedGids);
            EmbedGlyf(writer, font, gidMap, descriptor);
        }

        var cidSet = BuildFullCidSet(gidMap.Count);
        var widths = BuildWidths(font, usedGids, gidMap);
        var toUnicode = RemapToCompactGids(gidToUnicode, gidMap);

        descriptor["CIDSet"] = writer.Add(FlateFilter.EncodeStream(cidSet));
        var descriptorRef = writer.Add(descriptor);

        var descendant = new DictionaryObject
        {
            ["Type"] = new NameObject("Font"),
            ["Subtype"] = new NameObject(font.IsCff ? "CIDFontType0" : "CIDFontType2"),
            ["BaseFont"] = new NameObject(baseName),
            ["CIDSystemInfo"] = new DictionaryObject
            {
                ["Registry"] = new StringObject("Adobe"),
                ["Ordering"] = new StringObject("Identity"),
                ["Supplement"] = new NumberObject(0),
            },
            ["FontDescriptor"] = descriptorRef,
            ["DW"] = new NumberObject(DefaultWidth),
            ["W"] = widths,
        };

        if (!font.IsCff)
        {
            descendant["CIDToGIDMap"] = new NameObject("Identity");
        }

        var descendantRef = writer.Add(descendant);
        var toUnicodeRef = writer.Add(FlateFilter.EncodeStream(BuildToUnicode(toUnicode)));

        var top = new DictionaryObject
        {
            ["Type"] = new NameObject("Font"),
            ["Subtype"] = new NameObject("Type0"),
            ["BaseFont"] = new NameObject(baseName),
            ["Encoding"] = new NameObject("Identity-H"),
            ["DescendantFonts"] = new ArrayObject { descendantRef },
            ["ToUnicode"] = toUnicodeRef,
        };

        return writer.Add(top);
    }

    private static void EmbedGlyf(DocumentWriter writer, SfntFont font, IReadOnlyDictionary<ushort, ushort> gidMap, DictionaryObject descriptor)
    {
        var subset = GlyfSubsetter.SubsetPooled(font, gidMap, out var subsetLength);
        try
        {
            var stream = FlateFilter.EncodeStream(subset.AsSpan(0, subsetLength));
            stream.Dictionary["Length1"] = new NumberObject(subsetLength);
            descriptor["FontFile2"] = writer.Add(stream);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(subset);
        }
    }

    private static void EmbedCff(DocumentWriter writer, SfntFont font, SortedSet<ushort> usedGids, DictionaryObject descriptor)
    {
        if (!font.TryGetTable("CFF ", out var cffData))
        {
            throw new InvalidOperationException("Font reports CFF outlines but has no 'CFF ' table.");
        }

        var cff = CffFont.Parse(cffData);
        var gids = new List<int>(usedGids.Count);
        foreach (var gid in usedGids)
        {
            gids.Add(gid);
        }

        var subset = CffSubsetter.Subset(cff, gids);
        var stream = FlateFilter.EncodeStream(subset);
        stream.Dictionary["Subtype"] = new NameObject("CIDFontType0C");
        descriptor["FontFile3"] = writer.Add(stream);
    }

    public static Dictionary<ushort, int> RemapToCompactGids(IReadOnlyDictionary<ushort, int> gidToUnicode, IReadOnlyDictionary<ushort, ushort> gidMap)
    {
        var remapped = new Dictionary<ushort, int>(gidToUnicode.Count);
        foreach (var (gid, codepoint) in gidToUnicode)
        {
            remapped[gidMap[gid]] = codepoint;
        }

        return remapped;
    }

    private static ArrayObject BuildWidths(SfntFont font, SortedSet<ushort> usedGids, IReadOnlyDictionary<ushort, ushort> gidMap)
    {
        var w = new ArrayObject();
        ArrayObject? run = null;
        var prev = -1;
        foreach (var gid in usedGids)
        {
            var cid = gidMap[gid];
            var width = (int)Math.Round(
                FontMetric.Scale(font.GetAdvanceWidth(gid), 1000, font.UnitsPerEm),
                MidpointRounding.AwayFromZero);
            if (run is not null && cid == prev + 1)
            {
                run.Add(new NumberObject(width));
            }
            else
            {
                run = [new NumberObject(width)];
                w.Add(new NumberObject(cid));
                w.Add(run);
            }

            prev = cid;
        }

        return w;
    }

    private static byte[] BuildFullCidSet(int glyphCount)
    {
        var bytes = new byte[(glyphCount + 7) >> 3];
        for (var cid = 0; cid < glyphCount; cid++)
        {
            bytes[cid >> 3] |= (byte)(0x80 >> (cid & 7));
        }

        return bytes;
    }

    private static byte[] BuildToUnicode(IReadOnlyDictionary<ushort, int> gidToUnicode)
    {
        var entries = new List<KeyValuePair<ushort, int>>(gidToUnicode);
        entries.Sort((a, b) => a.Key.CompareTo(b.Key));

        using var output = new PooledBufferStream(512 + entries.Count * 16);
        PdfBytes.WriteAscii(output, "/CIDInit /ProcSet findresource begin\n12 dict begin\nbegincmap\n");
        PdfBytes.WriteAscii(output, "/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def\n");
        PdfBytes.WriteAscii(output, "/CMapName /Adobe-Identity-UCS def\n/CMapType 2 def\n");
        PdfBytes.WriteAscii(output, "1 begincodespacerange\n<0000> <FFFF>\nendcodespacerange\n");

        for (var offset = 0; offset < entries.Count; offset += 100)
        {
            var count = Math.Min(100, entries.Count - offset);
            PdfBytes.WriteInteger(output, count);
            PdfBytes.WriteAscii(output, " beginbfchar\n");
            for (var i = 0; i < count; i++)
            {
                var entry = entries[offset + i];
                output.WriteByte((byte)'<');
                WriteHex4(output, entry.Key);
                PdfBytes.WriteAscii(output, "> <");
                WriteUtf16BeHex(output, entry.Value);
                PdfBytes.WriteAscii(output, ">\n");
            }

            PdfBytes.WriteAscii(output, "endbfchar\n");
        }

        PdfBytes.WriteAscii(output, "endcmap\nCMapName currentdict /CMap defineresource pop\nend\nend\n");
        return output.ToArray();
    }

    private static void WriteUtf16BeHex(Stream stream, int codepoint)
    {
        if (codepoint is < 0 or > 0x10FFFF or (>= 0xD800 and <= 0xDFFF))
        {
            WriteHex4(stream, codepoint & 0xFFFF);
        }
        else if (codepoint <= 0xFFFF)
        {
            WriteHex4(stream, codepoint);
        }
        else
        {
            var v = codepoint - 0x10000;
            WriteHex4(stream, 0xD800 + (v >> 10));
            WriteHex4(stream, 0xDC00 + (v & 0x3FF));
        }
    }

    private static void WriteHex4(Stream stream, int value)
    {
        Span<byte> hex = stackalloc byte[4];
        for (var i = 3; i >= 0; i--)
        {
            var nibble = value & 0xF;
            hex[i] = (byte)(nibble < 10 ? '0' + nibble : 'A' + nibble - 10);
            value >>= 4;
        }

        stream.Write(hex);
    }

    private static ArrayObject FontBBox(SfntFont font, double scale)
    {
        short xMin = 0, yMin = 0, xMax = 0, yMax = 0;
        if (font.TryGetTable("head", out var head) && head.Length >= 44)
        {
            xMin = ReadInt16(head, 36);
            yMin = ReadInt16(head, 38);
            xMax = ReadInt16(head, 40);
            yMax = ReadInt16(head, 42);
        }

        return
        [
            new NumberObject(Scale(xMin, scale)),
            new NumberObject(Scale(yMin, scale)),
            new NumberObject(Scale(xMax, scale)),
            new NumberObject(Scale(yMax, scale)),
        ];
    }

    private static int CapHeight(SfntFont font, double scale)
    {
        var cap = font.CapHeight != 0 ? Scale(font.CapHeight, scale) : (int)Math.Round(Scale(font.Ascent, scale) * 0.7);
        return cap > 0 ? cap : 1;
    }

    private static int Scale(int value, double scale) => (int)Math.Round(value * scale, MidpointRounding.AwayFromZero);

    private static short ReadInt16(byte[] data, int offset) => (short)((data[offset] << 8) | data[offset + 1]);


    private static string SubsetTag(IEnumerable<ushort> gids)
    {
        uint hash = 2166136261;
        foreach (var gid in gids)
        {
            hash = (hash ^ gid) * 16777619;
        }

        var tag = new char[6];
        for (var i = 0; i < 6; i++)
        {
            tag[i] = (char)('A' + (hash % 26));
            hash /= 26;
        }

        return new string(tag);
    }
}
