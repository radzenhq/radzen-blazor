#nullable enable
using System;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Pdf.Fonts.Sfnt;

// Rebuilds a TrueType font as a COMPACT subset: glyphs are renumbered into a
// contiguous 0..N-1 space holding exactly the closure of the requested set
// (requested + recursive composite components + .notdef), with compact
// loca/glyf/hmtx sized for N glyphs. New gids are assigned in ascending original
// gid order, so the mapping is deterministic and monotonic. Composite component
// ids are rewritten into the compact space. cmap/name and layout tables are
// dropped (a Type0/CIDFontType2 embedded subset needs neither).
internal static class GlyfSubsetter
{
    private const ushort MoreComponents = 0x0020;
    private const ushort Arg1And2AreWords = 0x0001;
    private const ushort WeHaveAScale = 0x0008;
    private const ushort XAndYScale = 0x0040;
    private const ushort TwoByTwo = 0x0080;
    private const uint ChecksumMagic = 0xB1B0AFBA;
    private static readonly string[] HintingTables = ["cvt ", "fpgm", "prep"];

    public static byte[] Subset(SfntFont font, IReadOnlyCollection<ushort> glyphIds)
    {
        var rented = SubsetPooled(font, glyphIds, out var length, out _);
        try
        {
            return rented.AsSpan(0, length).ToArray();
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(rented);
        }
    }

    // The compact renumbering for a request: original gid -> new gid, covering the
    // full glyf closure plus .notdef, assigned in ascending original-gid order.
    // Deterministic, so content generation and embedding agree on the codes.
    public static Dictionary<ushort, ushort> BuildCompactGidMap(SfntFont font, IReadOnlyCollection<ushort> glyphIds)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(glyphIds);

        RequireTrueType(font, out var glyfMemory, out var locaMemory, out var headMemory);
        var loca = new LocaTable(locaMemory.Span, ReadInt16(headMemory.Span, 50) != 0);
        return BuildMap(OrderedClosure(glyfMemory.Span, loca, font.GlyphCount, glyphIds));
    }

    // Returns a pooled array holding the subset in its first length bytes; the
    // caller must return it to ArrayPool<byte>.Shared. gidMap receives the
    // original-to-compact renumbering (same as BuildCompactGidMap).
    public static byte[] SubsetPooled(SfntFont font, IReadOnlyCollection<ushort> glyphIds, out int length, out Dictionary<ushort, ushort> gidMap)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(glyphIds);

        RequireTrueType(font, out var glyfMemory, out var locaMemory, out var headMemory);

        var glyf = glyfMemory.Span;
        var head = headMemory.Span;
        var loca = new LocaTable(locaMemory.Span, ReadInt16(head, 50) != 0);

        var ordered = OrderedClosure(glyf, loca, font.GlyphCount, glyphIds);
        gidMap = BuildMap(ordered);
        var count = ordered.Count;

        var glyfLength = 0;
        foreach (var gid in ordered)
        {
            glyfLength += (int)(loca[gid + 1] - loca[gid]);
        }

        var pool = System.Buffers.ArrayPool<byte>.Shared;
        var newGlyf = pool.Rent(Math.Max(glyfLength, 1));
        var newLoca = new byte[(count + 1) * 4];
        try
        {
            FillGlyf(glyf, loca, ordered, gidMap, newGlyf, newLoca);
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
            // A corrupt font (e.g. a cmap returning a gid past the glyph count) must
            // fail loudly here rather than silently drop the glyph and later throw an
            // opaque KeyNotFoundException while remapping.
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
                continue; // simple glyph
            }

            var pos = (int)start + 10;
            while (true)
            {
                var flags = ReadUInt16(glyf, pos);
                Enqueue(ReadUInt16(glyf, pos + 2));
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

    private static void FillGlyf(ReadOnlySpan<byte> glyf, LocaTable loca, List<ushort> ordered, Dictionary<ushort, ushort> gidMap, byte[] newGlyf, byte[] newLoca)
    {
        var offset = 0;
        for (var newGid = 0; newGid < ordered.Count; newGid++)
        {
            WriteUInt32(newLoca, newGid * 4, (uint)offset);
            var gid = ordered[newGid];
            var start = (int)loca[gid];
            var length = (int)(loca[gid + 1] - loca[gid]);
            glyf.Slice(start, length).CopyTo(newGlyf.AsSpan(offset));

            if (length >= 10 && ReadInt16(glyf, start) < 0)
            {
                RewriteComponents(newGlyf, offset, gidMap);
            }

            offset += length;
        }

        WriteUInt32(newLoca, ordered.Count * 4, (uint)offset);
    }

    private static void RewriteComponents(byte[] outline, int glyphOffset, Dictionary<ushort, ushort> gidMap)
    {
        var pos = glyphOffset + 10;
        while (true)
        {
            var flags = ReadUInt16(outline, pos);
            var component = ReadUInt16(outline, pos + 2);
            WriteUInt16(outline, pos + 2, gidMap[component]);
            pos += ComponentRecordTail(flags) + 4;

            if ((flags & MoreComponents) == 0)
            {
                break;
            }
        }
    }

    private static byte[] BuildHead(ReadOnlySpan<byte> head)
    {
        var result = head.ToArray();
        WriteUInt32(result, 8, 0); // checkSumAdjustment, finalized after assembly
        WriteInt16(result, 50, 1); // indexToLocFormat = long
        return result;
    }

    // Full 4-byte (advance, lsb) entries for all N glyphs; hhea.numberOfHMetrics
    // is patched to N to match.
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

        // Glyph outlines are copied verbatim including their instruction bytecode,
        // which CALLs fpgm functions and reads cvt; carry those and prep through so
        // hint-executing rasterizers do not error on missing data.
        foreach (var tag in HintingTables)
        {
            if (font.TryGetTableMemory(tag, out var hinting))
            {
                tables.Add((tag, hinting));
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
        var cursor = directorySize;
        for (var i = 0; i < numTables; i++)
        {
            offsets[i] = cursor;
            cursor += Align4(tables[i].Data.Length);
        }

        length = cursor;
        var pool = System.Buffers.ArrayPool<byte>.Shared;
        var file = pool.Rent(cursor);

        // The caller owns the buffer on success; a throw before that must return it.
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
