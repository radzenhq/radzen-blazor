using System;
using System.Buffers.Binary;
using System.IO;
using System.Threading;

namespace Radzen.Documents.Fonts.Sfnt;

internal sealed class Cmap
{
    private const int MemoSize = 0x800;

    private readonly ICmapSubtable subtable;
    private readonly bool symbol;
    private int[]? memo;

    private Cmap(ICmapSubtable subtable, bool symbol)
    {
        this.subtable = subtable;
        this.symbol = symbol;
    }

    public ushort GetGlyphId(int codepoint)
    {
        if ((uint)codepoint < MemoSize)
        {
            var cache = Volatile.Read(ref memo);
            if (cache is null)
            {
                var created = new int[MemoSize];
                cache = Interlocked.CompareExchange(ref memo, created, null) ?? created;
            }

            var value = Volatile.Read(ref cache[codepoint]);
            if (value == 0)
            {
                var resolved = Lookup(codepoint) + 1;
                value = Interlocked.CompareExchange(ref cache[codepoint], resolved, 0);
                if (value == 0)
                {
                    value = resolved;
                }
            }

            return (ushort)(value - 1);
        }

        return Lookup(codepoint);
    }

    private ushort Lookup(int codepoint)
    {
        var glyph = subtable.GetGlyphId(codepoint);

        if (glyph == 0 && symbol && (uint)codepoint <= 0xFF)
        {
            glyph = subtable.GetGlyphId(0xF000 | codepoint);
        }

        return glyph;
    }

    public static Cmap Parse(byte[] cmapTable)
    {
        ArgumentNullException.ThrowIfNull(cmapTable);
        return Parse(cmapTable, 0);
    }

    public static Cmap Parse(byte[] data, int tableOffset)
    {
        var reader = new SfntReader(data, tableOffset);
        reader.ReadUInt16();
        var numTables = reader.ReadUInt16();

        var bestOffset = -1;
        var bestFormat = 0;
        var bestScore = -1;
        var bestSymbol = false;

        for (var i = 0; i < numTables; i++)
        {
            var platformId = reader.ReadUInt16();
            var encodingId = reader.ReadUInt16();
            var relative = reader.ReadUInt32();
            if (relative > int.MaxValue || (long)tableOffset + relative > data.Length)
            {
                throw new InvalidDataException("A cmap subtable offset lies outside the font data.");
            }

            var subtableOffset = tableOffset + (int)relative;
            var next = reader.Position;

            var format = new SfntReader(data, subtableOffset).ReadUInt16();
            if (format is 4 or 12)
            {
                var score = Score(platformId, encodingId, format);
                if (score > bestScore)
                {
                    bestOffset = subtableOffset;
                    bestFormat = format;
                    bestScore = score;
                    bestSymbol = platformId == 3 && encodingId == 0;
                }
            }

            reader.Position = next;
        }

        if (bestOffset < 0)
        {
            throw new InvalidDataException("No supported cmap subtable (format 4 or 12) found.");
        }

        ICmapSubtable best = bestFormat == 4
            ? Format4Subtable.Parse(data, bestOffset)
            : Format12Subtable.Parse(data, bestOffset);

        return new Cmap(best, bestSymbol);
    }

    private static int Score(int platformId, int encodingId, int format)
    {
        var score = format == 12 ? 200 : 100;

        if (platformId == 3 && (encodingId == 10 || encodingId == 1))
        {
            score += 20;
        }
        else if (platformId == 0)
        {
            score += 15;
        }
        else if (platformId == 3 && encodingId == 0)
        {
            score += 5;
        }

        return score;
    }
}

internal interface ICmapSubtable
{
    ushort GetGlyphId(int codepoint);
}

internal sealed class Format4Subtable : ICmapSubtable
{
    private readonly ushort[] endCode;
    private readonly ushort[] startCode;
    private readonly short[] idDelta;
    private readonly ushort[] idRangeOffset;
    private readonly byte[] data;
    private readonly int idRangeOffsetBase;

    private Format4Subtable(ushort[] endCode, ushort[] startCode, short[] idDelta,
        ushort[] idRangeOffset, byte[] data, int idRangeOffsetBase)
    {
        this.endCode = endCode;
        this.startCode = startCode;
        this.idDelta = idDelta;
        this.idRangeOffset = idRangeOffset;
        this.data = data;
        this.idRangeOffsetBase = idRangeOffsetBase;
    }

    public static Format4Subtable Parse(byte[] data, int offset)
    {
        var reader = new SfntReader(data, offset);
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();
        var segCount = reader.ReadUInt16() / 2;

        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt16();

        var endCode = new ushort[segCount];
        for (var i = 0; i < segCount; i++)
        {
            endCode[i] = reader.ReadUInt16();
        }

        reader.ReadUInt16();

        var startCode = new ushort[segCount];
        for (var i = 0; i < segCount; i++)
        {
            startCode[i] = reader.ReadUInt16();
        }

        var idDelta = new short[segCount];
        for (var i = 0; i < segCount; i++)
        {
            idDelta[i] = reader.ReadInt16();
        }

        var idRangeOffsetBase = reader.Position;
        var idRangeOffset = new ushort[segCount];
        for (var i = 0; i < segCount; i++)
        {
            idRangeOffset[i] = reader.ReadUInt16();
        }

        RequireAscending(endCode);
        return new Format4Subtable(endCode, startCode, idDelta, idRangeOffset, data, idRangeOffsetBase);
    }

    // ISO/IEC 14496-22 5.2.1.3.1: the format 4 segments are sorted by increasing endCode, which the
    // binary search in GetGlyphId relies on.
    private static void RequireAscending(ushort[] endCode)
    {
        for (var i = 1; i < endCode.Length; i++)
        {
            if (endCode[i] <= endCode[i - 1])
            {
                throw new InvalidDataException("cmap format 4 segments are not sorted by increasing end code.");
            }
        }
    }

    public ushort GetGlyphId(int codepoint)
    {
        if (codepoint < 0 || codepoint > 0xFFFF)
        {
            return 0;
        }

        if (endCode.Length == 0)
        {
            return 0;
        }

        var c = (ushort)codepoint;

        var lo = 0;
        var hi = endCode.Length - 1;
        while (lo < hi)
        {
            var mid = (lo + hi) / 2;
            if (endCode[mid] < c)
            {
                lo = mid + 1;
            }
            else
            {
                hi = mid;
            }
        }

        if (c > endCode[lo] || c < startCode[lo])
        {
            return 0;
        }

        if (idRangeOffset[lo] == 0)
        {
            return (ushort)((c + idDelta[lo]) & 0xFFFF);
        }

        var glyphIndexOffset = idRangeOffsetBase + (lo * 2) + idRangeOffset[lo] + ((c - startCode[lo]) * 2);
        if (glyphIndexOffset < 0 || glyphIndexOffset + 2 > data.Length)
        {
            throw new InvalidDataException("Attempt to read past the end of the sfnt data.");
        }

        var glyph = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(glyphIndexOffset));
        if (glyph == 0)
        {
            return 0;
        }

        return (ushort)((glyph + idDelta[lo]) & 0xFFFF);
    }
}

internal sealed class Format12Subtable : ICmapSubtable
{
    private readonly uint[] startCharCode;
    private readonly uint[] endCharCode;
    private readonly uint[] startGlyphId;

    private Format12Subtable(uint[] startCharCode, uint[] endCharCode, uint[] startGlyphId)
    {
        this.startCharCode = startCharCode;
        this.endCharCode = endCharCode;
        this.startGlyphId = startGlyphId;
    }

    public static Format12Subtable Parse(byte[] data, int offset)
    {
        var reader = new SfntReader(data, offset);
        reader.ReadUInt16();
        reader.ReadUInt16();
        reader.ReadUInt32();
        reader.ReadUInt32();
        var numGroups = reader.ReadUInt32();

        if (numGroups > int.MaxValue || (long)offset + 16 + ((long)numGroups * 12) > data.Length)
        {
            throw new InvalidDataException("cmap format 12 group count exceeds the subtable bounds.");
        }

        var startCharCode = new uint[numGroups];
        var endCharCode = new uint[numGroups];
        var startGlyphId = new uint[numGroups];
        for (var i = 0; i < numGroups; i++)
        {
            startCharCode[i] = reader.ReadUInt32();
            endCharCode[i] = reader.ReadUInt32();
            startGlyphId[i] = reader.ReadUInt32();
        }

        Validate(startCharCode, endCharCode, startGlyphId);
        return new Format12Subtable(startCharCode, endCharCode, startGlyphId);
    }

    // ISO/IEC 14496-22 5.2.1.3.7: the format 12 groups are sorted by increasing start character code and
    // do not overlap, which the binary search in GetGlyphId relies on.
    private static void Validate(uint[] startCharCode, uint[] endCharCode, uint[] startGlyphId)
    {
        for (var i = 0; i < startCharCode.Length; i++)
        {
            if (endCharCode[i] < startCharCode[i])
            {
                throw new InvalidDataException("A cmap format 12 group ends before it starts.");
            }

            if (i > 0 && startCharCode[i] <= endCharCode[i - 1])
            {
                throw new InvalidDataException("cmap format 12 groups are not sorted by increasing start character code.");
            }

            if (startGlyphId[i] + (long)(endCharCode[i] - startCharCode[i]) > ushort.MaxValue)
            {
                throw new InvalidDataException("A cmap format 12 group maps a character to a glyph index above 65535.");
            }
        }
    }

    public ushort GetGlyphId(int codepoint)
    {
        if (codepoint < 0)
        {
            return 0;
        }

        var c = (uint)codepoint;
        var lo = 0;
        var hi = startCharCode.Length - 1;
        while (lo <= hi)
        {
            var mid = (lo + hi) / 2;
            if (c < startCharCode[mid])
            {
                hi = mid - 1;
            }
            else if (c > endCharCode[mid])
            {
                lo = mid + 1;
            }
            else
            {
                return (ushort)(startGlyphId[mid] + (c - startCharCode[mid]));
            }
        }

        return 0;
    }
}
