#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Pdf.Fonts.Sfnt;

// Rebuilds a TrueType font retaining only the glyf outlines in the closure of a
// requested glyph set (identity glyph IDs, PDF CIDFontType2). numGlyphs, hmtx,
// cmap, name and metadata tables pass through unchanged; layout tables are dropped.
internal static class GlyfSubsetter
{
    private const ushort MoreComponents = 0x0020;
    private const ushort Arg1And2AreWords = 0x0001;
    private const ushort WeHaveAScale = 0x0008;
    private const ushort XAndYScale = 0x0040;
    private const ushort TwoByTwo = 0x0080;
    private const uint ChecksumMagic = 0xB1B0AFBA;

    private static readonly string[] RetainedTags =
    [
        "cmap", "glyf", "head", "hhea", "hmtx", "loca", "maxp", "name", "OS/2", "post",
    ];

    public static byte[] Subset(SfntFont font, IReadOnlyCollection<ushort> glyphIds)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(glyphIds);

        if (font.IsCff)
        {
            throw new NotSupportedException("glyf subsetting requires TrueType outlines; this font uses CFF outlines.");
        }

        if (!font.TryGetTable("glyf", out var glyf)
            || !font.TryGetTable("loca", out var locaRaw)
            || !font.TryGetTable("head", out var head))
        {
            throw new InvalidDataException("Font is missing required TrueType tables (glyf/loca/head).");
        }

        var numGlyphs = font.GlyphCount;
        var longLoca = ReadInt16(head, 50) != 0;
        var loca = ReadLoca(locaRaw, numGlyphs, longLoca);

        var closure = ComputeClosure(glyf, loca, numGlyphs, glyphIds);

        var newGlyf = BuildGlyf(glyf, loca, numGlyphs, closure, out var newLoca);
        var newHead = BuildHead(head);

        return Assemble(font, newGlyf, newLoca, newHead);
    }

    private static uint[] ReadLoca(byte[] locaRaw, ushort numGlyphs, bool longLoca)
    {
        var count = numGlyphs + 1;
        var offsets = new uint[count];
        for (var i = 0; i < count; i++)
        {
            offsets[i] = longLoca
                ? ReadUInt32(locaRaw, i * 4)
                : (uint)ReadUInt16(locaRaw, i * 2) * 2;
        }

        return offsets;
    }

    private static HashSet<ushort> ComputeClosure(byte[] glyf, uint[] loca, ushort numGlyphs, IReadOnlyCollection<ushort> requested)
    {
        var closure = new HashSet<ushort>();
        var pending = new Stack<ushort>();

        void Enqueue(ushort gid)
        {
            if (gid < numGlyphs && closure.Add(gid))
            {
                pending.Push(gid);
            }
        }

        Enqueue(0);
        foreach (var gid in requested)
        {
            Enqueue(gid);
        }

        while (pending.Count > 0)
        {
            var gid = pending.Pop();
            var start = loca[gid];
            var end = loca[gid + 1];
            if (end - start < 10)
            {
                continue;
            }

            if (ReadInt16(glyf, (int)start) >= 0)
            {
                continue; // simple glyph
            }

            var pos = (int)start + 10;
            while (true)
            {
                var flags = ReadUInt16(glyf, pos);
                var component = ReadUInt16(glyf, pos + 2);
                Enqueue(component);
                pos += 4;

                pos += (flags & Arg1And2AreWords) != 0 ? 4 : 2;
                if ((flags & WeHaveAScale) != 0)
                {
                    pos += 2;
                }
                else if ((flags & XAndYScale) != 0)
                {
                    pos += 4;
                }
                else if ((flags & TwoByTwo) != 0)
                {
                    pos += 8;
                }

                if ((flags & MoreComponents) == 0)
                {
                    break;
                }
            }
        }

        return closure;
    }

    private static byte[] BuildGlyf(byte[] glyf, uint[] loca, ushort numGlyphs, HashSet<ushort> closure, out byte[] newLoca)
    {
        var total = 0;
        for (ushort gid = 0; gid < numGlyphs; gid++)
        {
            if (closure.Contains(gid))
            {
                total += (int)(loca[gid + 1] - loca[gid]);
            }
        }

        var result = new byte[total];
        newLoca = new byte[(numGlyphs + 1) * 4];

        var offset = 0;
        for (ushort gid = 0; gid < numGlyphs; gid++)
        {
            WriteUInt32(newLoca, gid * 4, (uint)offset);
            if (closure.Contains(gid))
            {
                var start = (int)loca[gid];
                var length = (int)(loca[gid + 1] - loca[gid]);
                Array.Copy(glyf, start, result, offset, length);
                offset += length;
            }
        }

        WriteUInt32(newLoca, numGlyphs * 4, (uint)offset);
        return result;
    }

    private static byte[] BuildHead(byte[] head)
    {
        var result = (byte[])head.Clone();
        WriteUInt32(result, 8, 0); // checkSumAdjustment, finalized after assembly
        WriteInt16(result, 50, 1); // indexToLocFormat = long
        return result;
    }

    // Downgrade post to format 3.0: drop the per-glyph name array (tens of KB) but
    // keep the 32-byte header (italicAngle, underline metrics) intact.
    private static byte[] BuildPost(byte[] post)
    {
        var result = new byte[32];
        Array.Copy(post, 0, result, 0, Math.Min(32, post.Length));
        WriteUInt32(result, 0, 0x00030000);
        return result;
    }

    private static byte[] Assemble(SfntFont font, byte[] newGlyf, byte[] newLoca, byte[] newHead)
    {
        var tables = new List<(string Tag, byte[] Data)>(RetainedTags.Length);
        foreach (var tag in RetainedTags)
        {
            byte[]? data = tag switch
            {
                "glyf" => newGlyf,
                "loca" => newLoca,
                "head" => newHead,
                "post" => font.TryGetTable(tag, out var rawPost) ? BuildPost(rawPost) : null,
                _ => font.TryGetTable(tag, out var raw) ? raw : null,
            };

            if (data is not null)
            {
                tables.Add((tag, data));
            }
        }

        tables.Sort(static (a, b) => string.CompareOrdinal(a.Tag, b.Tag));

        var numTables = tables.Count;
        var maxPow2 = 1;
        var entrySelector = 0;
        while (maxPow2 * 2 <= numTables)
        {
            maxPow2 *= 2;
            entrySelector++;
        }

        var searchRange = maxPow2 * 16;
        var rangeShift = numTables * 16 - searchRange;

        var directorySize = 12 + numTables * 16;
        var offsets = new int[numTables];
        var checksums = new uint[numTables];
        var cursor = directorySize;
        for (var i = 0; i < numTables; i++)
        {
            offsets[i] = cursor;
            cursor += Align4(tables[i].Data.Length);
        }

        var file = new byte[cursor];

        WriteUInt32(file, 0, 0x00010000);
        WriteUInt16(file, 4, (ushort)numTables);
        WriteUInt16(file, 6, (ushort)searchRange);
        WriteUInt16(file, 8, (ushort)entrySelector);
        WriteUInt16(file, 10, (ushort)rangeShift);

        for (var i = 0; i < numTables; i++)
        {
            var (tag, data) = tables[i];
            Array.Copy(data, 0, file, offsets[i], data.Length);
            checksums[i] = TableChecksum(file, offsets[i], Align4(data.Length));

            var rec = 12 + i * 16;
            WriteTag(file, rec, tag);
            WriteUInt32(file, rec + 4, checksums[i]);
            WriteUInt32(file, rec + 8, (uint)offsets[i]);
            WriteUInt32(file, rec + 12, (uint)data.Length);
        }

        var headIndex = tables.FindIndex(static t => t.Tag == "head");
        var headOffset = offsets[headIndex];
        var adjustment = unchecked(ChecksumMagic - TableChecksum(file, 0, file.Length));
        WriteUInt32(file, headOffset + 8, adjustment);

        return file;
    }

    private static int Align4(int value) => (value + 3) & ~3;

    private static uint TableChecksum(byte[] data, int offset, int length)
    {
        uint sum = 0;
        for (var i = offset; i + 4 <= offset + length; i += 4)
        {
            sum = unchecked(sum + ReadUInt32(data, i));
        }

        return sum;
    }

    private static ushort ReadUInt16(byte[] d, int o) => (ushort)((d[o] << 8) | d[o + 1]);

    private static short ReadInt16(byte[] d, int o) => (short)((d[o] << 8) | d[o + 1]);

    private static uint ReadUInt32(byte[] d, int o)
        => ((uint)d[o] << 24) | ((uint)d[o + 1] << 16) | ((uint)d[o + 2] << 8) | d[o + 3];

    private static void WriteUInt16(byte[] d, int o, ushort v)
    {
        d[o] = (byte)(v >> 8);
        d[o + 1] = (byte)v;
    }

    private static void WriteInt16(byte[] d, int o, short v) => WriteUInt16(d, o, (ushort)v);

    private static void WriteUInt32(byte[] d, int o, uint v)
    {
        d[o] = (byte)(v >> 24);
        d[o + 1] = (byte)(v >> 16);
        d[o + 2] = (byte)(v >> 8);
        d[o + 3] = (byte)v;
    }

    private static void WriteTag(byte[] d, int o, string tag)
    {
        for (var i = 0; i < 4; i++)
        {
            d[o + i] = (byte)tag[i];
        }
    }
}
