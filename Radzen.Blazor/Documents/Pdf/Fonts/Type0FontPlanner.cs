using Radzen.Documents.Fonts;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents.Pdf.Emission;
using Radzen.Documents.Pdf.Fonts.Cff;
using Radzen.Documents.Pdf.Objects;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;

namespace Radzen.Documents.Pdf.Fonts;

// Type0/CID font subset and descriptor metrics per ISO 32000-1 9.7.
internal static class Type0FontPlanner
{
    public static EmissionFontProgram Plan(
        SfntFont font,
        IReadOnlyDictionary<ushort, int> gidToUnicode,
        IReadOnlyDictionary<ushort, ushort>? compactGidMap = null)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(gidToUnicode);

        var usedGids = new SortedSet<ushort>(gidToUnicode.Keys);
        var scale = 1000.0 / font.UnitsPerEm;
        var baseName = SubsetTag(usedGids) + "+"
            + (string.IsNullOrEmpty(font.PostScriptName) ? "Font" : font.PostScriptName);

        IReadOnlyDictionary<ushort, ushort> gidMap = compactGidMap ?? CompactGidMap.Build(font, usedGids);
        var (kind, file) = font.IsCff ? SubsetCff(font, usedGids) : SubsetGlyf(font, gidMap);

        return new EmissionFontProgram(
            kind,
            file,
            BuildFullCidSet(gidMap.Count),
            BuildWidths(font, usedGids, gidMap),
            BuildToUnicode(Type0FontEmbedder.RemapToCompactGids(gidToUnicode, gidMap)),
            baseName,
            font.Italic ? 96 : 32,
            FontBBox(font, scale),
            font.ItalicAngle,
            Scale(font.Ascent, scale),
            Scale(font.Descent, scale),
            CapHeight(font, scale));
    }

    private static (EmissionFontFileKind Kind, byte[] File) SubsetGlyf(
        SfntFont font,
        IReadOnlyDictionary<ushort, ushort> gidMap)
    {
        var subset = GlyfSubsetter.SubsetPooled(font, gidMap, out var subsetLength);
        try
        {
            return (EmissionFontFileKind.Glyf, subset.AsSpan(0, subsetLength).ToArray());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(subset);
        }
    }

    private static (EmissionFontFileKind Kind, byte[] File) SubsetCff(SfntFont font, SortedSet<ushort> usedGids)
    {
        if (!font.TryGetTable("CFF ", out var cffData))
        {
            throw new InvalidOperationException("Font reports CFF outlines but has no 'CFF ' table.");
        }

        var gids = new List<int>(usedGids.Count);
        foreach (var gid in usedGids)
        {
            gids.Add(gid);
        }

        return (EmissionFontFileKind.Cff, CffSubsetter.Subset(CffFont.Parse(cffData), gids));
    }

    private static ImmutableArray<EmissionWidthRun> BuildWidths(
        SfntFont font,
        SortedSet<ushort> usedGids,
        IReadOnlyDictionary<ushort, ushort> gidMap)
    {
        var runs = ImmutableArray.CreateBuilder<EmissionWidthRun>();
        List<int>? current = null;
        var cidOfRun = 0;
        var prev = -1;
        foreach (var gid in usedGids)
        {
            var cid = gidMap[gid];
            var width = (int)Math.Round(
                FontMetric.Scale(font.GetAdvanceWidth(gid), 1000, font.UnitsPerEm),
                MidpointRounding.AwayFromZero);
            if (current is not null && cid == prev + 1)
            {
                current.Add(width);
            }
            else
            {
                if (current is not null)
                {
                    runs.Add(new EmissionWidthRun(cidOfRun, [.. current]));
                }

                current = [width];
                cidOfRun = cid;
            }

            prev = cid;
        }

        if (current is not null)
        {
            runs.Add(new EmissionWidthRun(cidOfRun, [.. current]));
        }

        return runs.ToImmutable();
    }

    // CIDSet flags exactly the glyphs in the embedded subset (PDF/A 6.2.11.4.2).
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
        codepoint = UnicodeCodePoint.Sanitize(codepoint);
        if (codepoint <= 0xFFFF)
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

    private static ImmutableArray<int> FontBBox(SfntFont font, double scale)
    {
        short xMin = 0, yMin = 0, xMax = 0, yMax = 0;
        if (font.TryGetTable("head", out var head) && head.Length >= 44)
        {
            xMin = ReadInt16(head, 36);
            yMin = ReadInt16(head, 38);
            xMax = ReadInt16(head, 40);
            yMax = ReadInt16(head, 42);
        }

        return [Scale(xMin, scale), Scale(yMin, scale), Scale(xMax, scale), Scale(yMax, scale)];
    }

    private static int CapHeight(SfntFont font, double scale)
    {
        var cap = font.CapHeight != 0 ? Scale(font.CapHeight, scale) : (int)Math.Round(Scale(font.Ascent, scale) * 0.7);
        return cap > 0 ? cap : 1;
    }

    private static int Scale(int value, double scale) => (int)Math.Round(value * scale, MidpointRounding.AwayFromZero);

    private static short ReadInt16(byte[] data, int offset)
        => BigEndian.ReadInt16BigEndian(data, offset, "Font data is too short to contain its bounding box.");

    private static string SubsetTag(IEnumerable<ushort> gids)
    {
        var hash = Fnv1a32.OffsetBasis;
        foreach (var gid in gids)
        {
            hash = Fnv1a32.Combine(hash, gid);
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
