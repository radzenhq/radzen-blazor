#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Radzen.Documents.Pdf.Fonts.Cff;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;

namespace Radzen.Documents.Pdf.Fonts;

// Builds the Type0/CID font object graph (ISO 32000-1 9.7) for a used-glyph subset:
// a composite Type0 dictionary, a descendant CIDFontType2 (glyf) or CIDFontType0 (CFF),
// a FontDescriptor with an embedded FontFile2/FontFile3 subset, /W widths, a /CIDSet
// bitmap and a /ToUnicode CMap. Under Identity-H the CID equals the original glyph id.
internal static class Type0FontEmbedder
{
    private const int StemV = 80;
    private const int DefaultWidth = 1000;

    public static ReferenceObject Embed(DocumentWriter writer, SfntFont font, IReadOnlyDictionary<ushort, int> gidToUnicode)
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

        if (font.IsCff)
        {
            EmbedCff(writer, font, usedGids, descriptor);
        }
        else
        {
            EmbedGlyf(writer, font, usedGids, descriptor);
        }

        descriptor["CIDSet"] = writer.Add(FlateFilter.EncodeStream(BuildCidSet(usedGids)));
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
            ["W"] = BuildWidths(font, usedGids),
        };

        if (!font.IsCff)
        {
            descendant["CIDToGIDMap"] = new NameObject("Identity");
        }

        var descendantRef = writer.Add(descendant);
        var toUnicodeRef = writer.Add(FlateFilter.EncodeStream(BuildToUnicode(gidToUnicode)));

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

    private static void EmbedGlyf(DocumentWriter writer, SfntFont font, SortedSet<ushort> usedGids, DictionaryObject descriptor)
    {
        var subset = GlyfSubsetter.Subset(font, usedGids);
        var stream = FlateFilter.EncodeStream(subset);
        stream.Dictionary["Length1"] = new NumberObject(subset.Length);
        descriptor["FontFile2"] = writer.Add(stream);
    }

    private static void EmbedCff(DocumentWriter writer, SfntFont font, SortedSet<ushort> usedGids, DictionaryObject descriptor)
    {
        if (!font.TryGetTable("CFF ", out var cffData))
        {
            throw new InvalidOperationException("Font reports CFF outlines but has no 'CFF ' table.");
        }

        var cff = CffFont.Parse(cffData);

        // Under Identity-H the CID equals the glyph index we reference (the sfnt gid),
        // so the embedded CID font must map CID == gid. Force an identity charset before
        // subsetting; advance widths and FDSelect are unaffected.
        var charset = cff.Charset;
        for (var i = 0; i < charset.Length; i++)
        {
            charset[i] = i;
        }

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

    private static ArrayObject BuildWidths(SfntFont font, SortedSet<ushort> usedGids)
    {
        var w = new ArrayObject();
        foreach (var gid in usedGids)
        {
            var width = (int)Math.Round(font.GetAdvanceWidth(gid) * 1000.0 / font.UnitsPerEm, MidpointRounding.AwayFromZero);
            w.Add(new NumberObject(gid));
            w.Add(new ArrayObject { new NumberObject(width) });
        }

        return w;
    }

    private static byte[] BuildCidSet(SortedSet<ushort> usedGids)
    {
        var max = 0;
        foreach (var gid in usedGids)
        {
            if (gid > max)
            {
                max = gid;
            }
        }

        var bytes = new byte[(max >> 3) + 1];
        bytes[0] |= 0x80; // notdef (CID 0)
        foreach (var gid in usedGids)
        {
            bytes[gid >> 3] |= (byte)(0x80 >> (gid & 7));
        }

        return bytes;
    }

    private static byte[] BuildToUnicode(IReadOnlyDictionary<ushort, int> gidToUnicode)
    {
        var entries = new List<KeyValuePair<ushort, int>>(gidToUnicode);
        entries.Sort((a, b) => a.Key.CompareTo(b.Key));

        var sb = new StringBuilder();
        sb.Append("/CIDInit /ProcSet findresource begin\n12 dict begin\nbegincmap\n");
        sb.Append("/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def\n");
        sb.Append("/CMapName /Adobe-Identity-UCS def\n/CMapType 2 def\n");
        sb.Append("1 begincodespacerange\n<0000> <FFFF>\nendcodespacerange\n");

        for (var offset = 0; offset < entries.Count; offset += 100)
        {
            var count = Math.Min(100, entries.Count - offset);
            sb.Append(count.ToString(CultureInfo.InvariantCulture)).Append(" beginbfchar\n");
            for (var i = 0; i < count; i++)
            {
                var entry = entries[offset + i];
                sb.Append('<').Append(entry.Key.ToString("X4", CultureInfo.InvariantCulture)).Append("> <");
                sb.Append(Utf16BeHex(entry.Value)).Append(">\n");
            }

            sb.Append("endbfchar\n");
        }

        sb.Append("endcmap\nCMapName currentdict /CMap defineresource pop\nend\nend\n");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static string Utf16BeHex(int codepoint)
    {
        if (codepoint is < 0 or > 0x10FFFF or (>= 0xD800 and <= 0xDFFF))
        {
            // Lone surrogate (or out-of-range value): emit the raw UTF-16 unit.
            return (codepoint & 0xFFFF).ToString("X4", CultureInfo.InvariantCulture);
        }

        var s = char.ConvertFromUtf32(codepoint);
        var sb = new StringBuilder(s.Length * 4);
        foreach (var ch in s)
        {
            sb.Append(((int)ch).ToString("X4", CultureInfo.InvariantCulture));
        }

        return sb.ToString();
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
