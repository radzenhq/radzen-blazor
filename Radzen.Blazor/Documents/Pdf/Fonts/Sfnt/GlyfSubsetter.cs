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
        var rented = SubsetPooled(font, glyphIds, out var length);
        try
        {
            return rented.AsSpan(0, length).ToArray();
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(rented);
        }
    }

    // Returns a pooled array holding the subset in its first length bytes; the
    // caller must return it to ArrayPool<byte>.Shared.
    public static byte[] SubsetPooled(SfntFont font, IReadOnlyCollection<ushort> glyphIds, out int length)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(glyphIds);

        if (font.IsCff)
        {
            throw new NotSupportedException("glyf subsetting requires TrueType outlines; this font uses CFF outlines.");
        }

        if (!font.TryGetTableMemory("glyf", out var glyfMemory)
            || !font.TryGetTableMemory("loca", out var locaMemory)
            || !font.TryGetTableMemory("head", out var headMemory))
        {
            throw new InvalidDataException("Font is missing required TrueType tables (glyf/loca/head).");
        }

        var glyf = glyfMemory.Span;
        var head = headMemory.Span;
        var numGlyphs = font.GlyphCount;
        var longLoca = ReadInt16(head, 50) != 0;
        var loca = new LocaTable(locaMemory.Span, longLoca);

        var closure = ComputeClosure(glyf, loca, numGlyphs, glyphIds);

        var glyfLength = MeasureGlyf(loca, numGlyphs, closure);
        var locaLength = (numGlyphs + 1) * 4;
        var pool = System.Buffers.ArrayPool<byte>.Shared;
        var newGlyf = pool.Rent(glyfLength);
        var newLoca = pool.Rent(locaLength);
        try
        {
            FillGlyf(glyf, loca, numGlyphs, closure, newGlyf, newLoca);
            var newHead = BuildHead(head);
            return Assemble(font, newGlyf.AsMemory(0, glyfLength), newLoca.AsMemory(0, locaLength), newHead, out length);
        }
        finally
        {
            pool.Return(newGlyf);
            pool.Return(newLoca);
        }
    }

    // Reads glyph offsets straight from the raw loca table instead of
    // materializing a (numGlyphs + 1) uint array per subset.
    private readonly ref struct LocaTable(ReadOnlySpan<byte> raw, bool longFormat)
    {
        private readonly ReadOnlySpan<byte> raw = raw;
        private readonly bool longFormat = longFormat;

        public uint this[int index] => longFormat
            ? ReadUInt32(raw, index * 4)
            : (uint)ReadUInt16(raw, index * 2) * 2;
    }

    private static HashSet<ushort> ComputeClosure(ReadOnlySpan<byte> glyf, LocaTable loca, ushort numGlyphs, IReadOnlyCollection<ushort> requested)
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

    private static int MeasureGlyf(LocaTable loca, ushort numGlyphs, HashSet<ushort> closure)
    {
        var total = 0;
        for (ushort gid = 0; gid < numGlyphs; gid++)
        {
            if (closure.Contains(gid))
            {
                total += (int)(loca[gid + 1] - loca[gid]);
            }
        }

        return total;
    }

    private static void FillGlyf(ReadOnlySpan<byte> glyf, LocaTable loca, ushort numGlyphs, HashSet<ushort> closure, byte[] newGlyf, byte[] newLoca)
    {
        var offset = 0;
        for (ushort gid = 0; gid < numGlyphs; gid++)
        {
            WriteUInt32(newLoca, gid * 4, (uint)offset);
            if (closure.Contains(gid))
            {
                var start = (int)loca[gid];
                var length = (int)(loca[gid + 1] - loca[gid]);
                glyf.Slice(start, length).CopyTo(newGlyf.AsSpan(offset));
                offset += length;
            }
        }

        WriteUInt32(newLoca, numGlyphs * 4, (uint)offset);
    }

    private static byte[] BuildHead(ReadOnlySpan<byte> head)
    {
        var result = head.ToArray();
        WriteUInt32(result, 8, 0); // checkSumAdjustment, finalized after assembly
        WriteInt16(result, 50, 1); // indexToLocFormat = long
        return result;
    }

    // Downgrade post to format 3.0: drop the per-glyph name array (tens of KB) but
    // keep the 32-byte header (italicAngle, underline metrics) intact.
    private static byte[] BuildPost(ReadOnlySpan<byte> post)
    {
        var result = new byte[32];
        post[..Math.Min(32, post.Length)].CopyTo(result);
        WriteUInt32(result, 0, 0x00030000);
        return result;
    }

    private static byte[] Assemble(SfntFont font, ReadOnlyMemory<byte> newGlyf, ReadOnlyMemory<byte> newLoca, byte[] newHead, out int length)
    {
        var tables = new List<(string Tag, ReadOnlyMemory<byte> Data)>(RetainedTags.Length);
        foreach (var tag in RetainedTags)
        {
            ReadOnlyMemory<byte>? data = tag switch
            {
                "glyf" => newGlyf,
                "loca" => newLoca,
                "head" => newHead,
                "post" => font.TryGetTableMemory(tag, out var rawPost) ? BuildPost(rawPost.Span) : null,
                _ => font.TryGetTableMemory(tag, out var raw) ? raw : null,
            };

            if (data is { } table)
            {
                tables.Add((tag, table));
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

        length = cursor;
        var file = System.Buffers.ArrayPool<byte>.Shared.Rent(cursor);

        WriteUInt32(file, 0, 0x00010000);
        WriteUInt16(file, 4, (ushort)numTables);
        WriteUInt16(file, 6, (ushort)searchRange);
        WriteUInt16(file, 8, (ushort)entrySelector);
        WriteUInt16(file, 10, (ushort)rangeShift);

        for (var i = 0; i < numTables; i++)
        {
            var (tag, data) = tables[i];
            data.Span.CopyTo(file.AsSpan(offsets[i]));
            file.AsSpan(offsets[i] + data.Length, Align4(data.Length) - data.Length).Clear();
            checksums[i] = TableChecksum(file, offsets[i], Align4(data.Length));

            var rec = 12 + i * 16;
            WriteTag(file, rec, tag);
            WriteUInt32(file, rec + 4, checksums[i]);
            WriteUInt32(file, rec + 8, (uint)offsets[i]);
            WriteUInt32(file, rec + 12, (uint)data.Length);
        }

        var headIndex = tables.FindIndex(static t => t.Tag == "head");
        var headOffset = offsets[headIndex];
        var adjustment = unchecked(ChecksumMagic - TableChecksum(file, 0, cursor));
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

    private static ushort ReadUInt16(ReadOnlySpan<byte> d, int o) => (ushort)((d[o] << 8) | d[o + 1]);

    private static short ReadInt16(ReadOnlySpan<byte> d, int o) => (short)((d[o] << 8) | d[o + 1]);

    private static uint ReadUInt32(ReadOnlySpan<byte> d, int o)
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
