using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Fonts.Sfnt;

namespace Radzen.Documents.Pdf.Fonts;

internal static class GlyfSubsetter
{
    private const ushort MoreComponents = 0x0020;
    private const ushort Arg1And2AreWords = 0x0001;
    private const ushort WeHaveAScale = 0x0008;
    private const ushort XAndYScale = 0x0040;
    private const ushort TwoByTwo = 0x0080;
    private const ushort WeHaveInstructions = 0x0100;
    private const uint ChecksumMagic = 0xB1B0AFBA;

    public static byte[] Subset(SfntFont font, IReadOnlyCollection<ushort> glyphIds)
    {
        var gidMap = BuildCompactGidMap(font, glyphIds);
        var rented = SubsetPooled(font, gidMap, out var length);
        try
        {
            return rented.AsSpan(0, length).ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    public static Dictionary<ushort, ushort> BuildCompactGidMap(SfntFont font, IReadOnlyCollection<ushort> glyphIds)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(glyphIds);

        RequireTrueType(font, out var glyfMemory, out var locaMemory, out var headMemory);
        var loca = new LocaTable(locaMemory.Span, ReadInt16(headMemory.Span, 50) != 0);
        return BuildMap(OrderedClosure(glyfMemory.Span, loca, font.GlyphCount, glyphIds));
    }

    public static byte[] SubsetPooled(SfntFont font, IReadOnlyDictionary<ushort, ushort> gidMap, out int length)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(gidMap);

        RequireTrueType(font, out var glyfMemory, out var locaMemory, out var headMemory);

        var glyf = glyfMemory.Span;
        var head = headMemory.Span;
        var loca = new LocaTable(locaMemory.Span, ReadInt16(head, 50) != 0);

        var ordered = CompactGidMap.OrderFromMap(gidMap);
        var count = ordered.Count;

        var glyfUpperBound = 0;
        foreach (var gid in ordered)
        {
            glyfUpperBound += (int)(loca[gid + 1] - loca[gid]) + 1;
        }

        var pool = ArrayPool<byte>.Shared;
        var newGlyf = pool.Rent(Math.Max(glyfUpperBound, 1));
        var newLoca = new byte[(count + 1) * 4];
        try
        {
            var glyfLength = FillGlyf(glyf, loca, ordered, gidMap, newGlyf, newLoca);
            var newHead = BuildHead(head);
            var newHmtx = BuildHmtx(font, ordered);
            var newHhea = BuildHhea(font, count);
            var newMaxp = BuildMaxp(font, count);
            return Assemble(font, newGlyf.AsMemory(0, glyfLength), newLoca, newHead, newHmtx, newHhea, newMaxp, out length);
        }
        finally
        {
            pool.Return(newGlyf);
        }
    }

    private static void RequireTrueType(SfntFont font, out ReadOnlyMemory<byte> glyf, out ReadOnlyMemory<byte> loca, out ReadOnlyMemory<byte> head)
    {
        if (font.IsCff)
        {
            throw new NotSupportedException("glyf subsetting requires TrueType outlines; this font uses CFF outlines.");
        }

        if (!font.TryGetTableMemory("glyf", out glyf)
            || !font.TryGetTableMemory("loca", out loca)
            || !font.TryGetTableMemory("head", out head))
        {
            throw new InvalidDataException("Font is missing required TrueType tables (glyf/loca/head).");
        }
    }

    private static Dictionary<ushort, ushort> BuildMap(List<ushort> ordered)
    {
        var map = new Dictionary<ushort, ushort>(ordered.Count);
        for (var i = 0; i < ordered.Count; i++)
        {
            map[ordered[i]] = (ushort)i;
        }

        return map;
    }

    private readonly ref struct LocaTable(ReadOnlySpan<byte> raw, bool longFormat)
    {
        private readonly ReadOnlySpan<byte> raw = raw;
        private readonly bool longFormat = longFormat;

        public uint this[int index] => longFormat
            ? ReadUInt32(raw, index * 4)
            : (uint)ReadUInt16(raw, index * 2) * 2;
    }

    private static List<ushort> OrderedClosure(ReadOnlySpan<byte> glyf, LocaTable loca, ushort numGlyphs, IReadOnlyCollection<ushort> requested)
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
            if (gid >= numGlyphs)
            {
                throw new ArgumentOutOfRangeException(nameof(requested), gid,
                    $"Requested glyph id {gid} is outside the font's glyph range [0, {numGlyphs}).");
            }

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
                continue;
            }

            var pos = (int)start + 10;
            while (true)
            {
                var flags = ReadUInt16(glyf, pos);
                var component = ReadUInt16(glyf, pos + 2);

                if (component >= numGlyphs)
                {
                    throw new InvalidDataException(
                        $"Composite glyph references component {component} outside the font's glyph range [0, {numGlyphs}).");
                }

                Enqueue(component);
                pos += ComponentRecordTail(flags) + 4;

                if ((flags & MoreComponents) == 0)
                {
                    break;
                }
            }
        }

        var ordered = new List<ushort>(closure);
        ordered.Sort();
        return ordered;
    }

    private static int ComponentRecordTail(ushort flags)
    {
        var tail = (flags & Arg1And2AreWords) != 0 ? 4 : 2;
        if ((flags & WeHaveAScale) != 0)
        {
            tail += 2;
        }
        else if ((flags & XAndYScale) != 0)
        {
            tail += 4;
        }
        else if ((flags & TwoByTwo) != 0)
        {
            tail += 8;
        }

        return tail;
    }

    private static int FillGlyf(ReadOnlySpan<byte> glyf, LocaTable loca, List<ushort> ordered, IReadOnlyDictionary<ushort, ushort> gidMap, byte[] newGlyf, byte[] newLoca)
    {
        var offset = 0;
        for (var newGid = 0; newGid < ordered.Count; newGid++)
        {
            WriteUInt32(newLoca, newGid * 4, (uint)offset);
            var gid = ordered[newGid];
            var start = (int)loca[gid];
            var length = (int)(loca[gid + 1] - loca[gid]);

            int written;
            if (length < 10)
            {
                glyf.Slice(start, length).CopyTo(newGlyf.AsSpan(offset));
                written = length;
            }
            else if (ReadInt16(glyf, start) < 0)
            {
                written = CopyCompositeStripped(glyf, start, length, gidMap, newGlyf, offset);
            }
            else
            {
                written = CopySimpleStripped(glyf, start, length, newGlyf, offset);
            }

            if ((written & 1) != 0)
            {
                newGlyf[offset + written] = 0;
                written++;
            }

            offset += written;
        }

        WriteUInt32(newLoca, ordered.Count * 4, (uint)offset);
        return offset;
    }

    private static int CopySimpleStripped(ReadOnlySpan<byte> glyf, int start, int length, byte[] newGlyf, int offset)
    {
        var numberOfContours = ReadInt16(glyf, start);
        var instrLenPos = start + 10 + numberOfContours * 2;
        var instrLen = ReadUInt16(glyf, instrLenPos);

        var headLen = instrLenPos + 2 - start;
        glyf.Slice(start, headLen).CopyTo(newGlyf.AsSpan(offset));
        WriteUInt16(newGlyf, offset + headLen - 2, 0);

        var tailStart = instrLenPos + 2 + instrLen;
        var tailLen = start + length - tailStart;
        glyf.Slice(tailStart, tailLen).CopyTo(newGlyf.AsSpan(offset + headLen));

        return headLen + tailLen;
    }

    private static int CopyCompositeStripped(ReadOnlySpan<byte> glyf, int start, int length, IReadOnlyDictionary<ushort, ushort> gidMap, byte[] newGlyf, int offset)
    {
        glyf.Slice(start, length).CopyTo(newGlyf.AsSpan(offset));

        var pos = offset + 10;
        int lastFlagsPos;
        ushort flags;
        do
        {
            lastFlagsPos = pos;
            flags = ReadUInt16(newGlyf, pos);
            var component = ReadUInt16(newGlyf, pos + 2);
            WriteUInt16(newGlyf, pos + 2, gidMap[component]);
            pos += ComponentRecordTail(flags) + 4;
        }
        while ((flags & MoreComponents) != 0);

        var lastFlags = ReadUInt16(newGlyf, lastFlagsPos);
        if ((lastFlags & WeHaveInstructions) != 0)
        {
            WriteUInt16(newGlyf, lastFlagsPos, (ushort)(lastFlags & ~WeHaveInstructions));
        }

        return pos - offset;
    }

    private static byte[] BuildHead(ReadOnlySpan<byte> head)
    {
        var result = head.ToArray();
        WriteUInt32(result, 8, 0);
        WriteInt16(result, 50, 1);
        return result;
    }

    private static byte[] BuildHmtx(SfntFont font, List<ushort> ordered)
    {
        var hasHmtx = font.TryGetTableMemory("hmtx", out var hmtxMemory);
        var numberOfHMetrics = 0;
        if (font.TryGetTableMemory("hhea", out var hheaMemory) && hheaMemory.Length >= 36)
        {
            numberOfHMetrics = ReadUInt16(hheaMemory.Span, 34);
        }

        var hmtx = hasHmtx ? hmtxMemory.Span : default;
        var result = new byte[ordered.Count * 4];
        for (var newGid = 0; newGid < ordered.Count; newGid++)
        {
            var gid = ordered[newGid];
            WriteUInt16(result, newGid * 4, font.GetAdvanceWidth(gid));
            WriteInt16(result, newGid * 4 + 2, LeftSideBearing(hmtx, numberOfHMetrics, gid));
        }

        return result;
    }

    private static short LeftSideBearing(ReadOnlySpan<byte> hmtx, int numberOfHMetrics, ushort gid)
    {
        var offset = gid < numberOfHMetrics
            ? gid * 4 + 2
            : numberOfHMetrics * 4 + (gid - numberOfHMetrics) * 2;
        return offset + 2 <= hmtx.Length ? ReadInt16(hmtx, offset) : (short)0;
    }

    private static byte[] BuildHhea(SfntFont font, int glyphCount)
    {
        if (!font.TryGetTableMemory("hhea", out var hhea) || hhea.Length < 36)
        {
            throw new InvalidDataException("Font is missing a valid 'hhea' table.");
        }

        var result = hhea.ToArray();
        WriteUInt16(result, 34, (ushort)glyphCount);
        return result;
    }

    private static byte[] BuildMaxp(SfntFont font, int glyphCount)
    {
        if (!font.TryGetTableMemory("maxp", out var maxp) || maxp.Length < 6)
        {
            throw new InvalidDataException("Font is missing a valid 'maxp' table.");
        }

        var result = maxp.ToArray();
        WriteUInt16(result, 4, (ushort)glyphCount);
        if (result.Length >= 26)
        {
            WriteUInt16(result, 24, 0);
        }

        return result;
    }

    private static byte[] BuildPost(ReadOnlySpan<byte> post)
    {
        var result = new byte[32];
        post[..Math.Min(32, post.Length)].CopyTo(result);
        WriteUInt32(result, 0, 0x00030000);
        return result;
    }

    private static byte[] Assemble(
        SfntFont font,
        ReadOnlyMemory<byte> newGlyf,
        ReadOnlyMemory<byte> newLoca,
        byte[] newHead,
        byte[] newHmtx,
        byte[] newHhea,
        byte[] newMaxp,
        out int length)
    {
        var tables = new List<(string Tag, ReadOnlyMemory<byte> Data)>(8)
        {
            ("glyf", newGlyf),
            ("head", newHead),
            ("hhea", newHhea),
            ("hmtx", newHmtx),
            ("loca", newLoca),
            ("maxp", newMaxp),
        };

        if (font.TryGetTableMemory("OS/2", out var os2))
        {
            tables.Add(("OS/2", os2));
        }

        if (font.TryGetTableMemory("post", out var post))
        {
            tables.Add(("post", BuildPost(post.Span)));
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
        var cursor = directorySize;
        for (var i = 0; i < numTables; i++)
        {
            offsets[i] = cursor;
            cursor += Align4(tables[i].Data.Length);
        }

        length = cursor;
        var pool = ArrayPool<byte>.Shared;
        var file = pool.Rent(cursor);

        try
        {
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

                var rec = 12 + i * 16;
                WriteTag(file, rec, tag);
                WriteUInt32(file, rec + 4, TableChecksum(file, offsets[i], Align4(data.Length)));
                WriteUInt32(file, rec + 8, (uint)offsets[i]);
                WriteUInt32(file, rec + 12, (uint)data.Length);
            }

            var headIndex = tables.FindIndex(static t => t.Tag == "head");
            var headOffset = offsets[headIndex];
            var adjustment = unchecked(ChecksumMagic - TableChecksum(file, 0, cursor));
            WriteUInt32(file, headOffset + 8, adjustment);

            return file;
        }
        catch
        {
            pool.Return(file);
            throw;
        }
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

    private const string Truncated = "Font glyf/loca table is truncated.";

    private static ushort ReadUInt16(ReadOnlySpan<byte> d, int o) => BigEndian.ReadUInt16BigEndian(d, o, Truncated);

    private static short ReadInt16(ReadOnlySpan<byte> d, int o) => BigEndian.ReadInt16BigEndian(d, o, Truncated);

    private static uint ReadUInt32(ReadOnlySpan<byte> d, int o) => BigEndian.ReadUInt32BigEndian(d, o, Truncated);

    private static void WriteUInt16(byte[] d, int o, ushort v) => PdfBytes.WriteBigEndian(d.AsSpan(o, 2), v);

    private static void WriteInt16(byte[] d, int o, short v) => PdfBytes.WriteBigEndian(d.AsSpan(o, 2), v);

    private static void WriteUInt32(byte[] d, int o, uint v) => PdfBytes.WriteBigEndian(d.AsSpan(o, 4), v);

    private static void WriteTag(byte[] d, int o, string tag)
    {
        for (var i = 0; i < 4; i++)
        {
            d[o + i] = (byte)tag[i];
        }
    }
}
