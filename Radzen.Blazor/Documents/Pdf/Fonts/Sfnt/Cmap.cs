using System;
using System.Buffers.Binary;
using System.IO;

namespace Radzen.Documents.Pdf.Fonts.Sfnt;

// Character-to-glyph mapper built from a whole cmap table. Supports formats 4
// and 12; when both are present the format 12 subtable wins (full Unicode range).
internal sealed class Cmap
{
    // Covers Latin, Latin Extended, Greek and Cyrillic; entries store glyph+1 so 0
    // means "not resolved yet". Writes are idempotent, so races are benign.
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
            var cache = memo ??= new int[MemoSize];
            var value = cache[codepoint];
            if (value == 0)
            {
                cache[codepoint] = value = Lookup(codepoint) + 1;
            }

            return (ushort)(value - 1);
        }

        return Lookup(codepoint);
    }

    private ushort Lookup(int codepoint)
    {
        var glyph = subtable.GetGlyphId(codepoint);

        // Microsoft symbol (3,0) fonts key their cmap in the F000-F0FF PUA, so a raw
        // .notdef miss on a 0x00-0xFF code is retried with the 0xF000 offset applied.
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
        reader.ReadUInt16(); // version
        var numTables = reader.ReadUInt16();

        ICmapSubtable? best = null;
        var bestScore = -1;
        var bestSymbol = false;

        for (var i = 0; i < numTables; i++)
        {
            var platformId = reader.ReadUInt16();
            var encodingId = reader.ReadUInt16();
            var subtableOffset = tableOffset + (int)reader.ReadUInt32();
            var next = reader.Position;

            var format = new SfntReader(data, subtableOffset).ReadUInt16();
            ICmapSubtable? parsed = format switch
            {
                4 => Format4Subtable.Parse(data, subtableOffset),
                12 => Format12Subtable.Parse(data, subtableOffset),
                _ => null,
            };

            if (parsed != null)
            {
                var score = Score(platformId, encodingId, format);
                if (score > bestScore)
                {
                    best = parsed;
                    bestScore = score;
                    bestSymbol = platformId == 3 && encodingId == 0;
                }
            }

            reader.Position = next;
        }

        if (best == null)
        {
            throw new InvalidDataException("No supported cmap subtable (format 4 or 12) found.");
        }

        return new Cmap(best, bestSymbol);
    }

    private static int Score(int platformId, int encodingId, int format)
    {
        // Prefer format 12 over 4, and Unicode/Windows encodings over others.
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
        reader.ReadUInt16(); // format
        reader.ReadUInt16(); // length
        reader.ReadUInt16(); // language
        var segCount = reader.ReadUInt16() / 2;

        reader.ReadUInt16(); // searchRange
        reader.ReadUInt16(); // entrySelector
        reader.ReadUInt16(); // rangeShift

        var endCode = new ushort[segCount];
        for (var i = 0; i < segCount; i++)
        {
            endCode[i] = reader.ReadUInt16();
        }

        reader.ReadUInt16(); // reservedPad

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

        return new Format4Subtable(endCode, startCode, idDelta, idRangeOffset, data, idRangeOffsetBase);
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

        // First segment whose endCode >= c, matching the spec's ordered-segment search.
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
        reader.ReadUInt16(); // format
        reader.ReadUInt16(); // reserved
        reader.ReadUInt32(); // length
        reader.ReadUInt32(); // language
        var numGroups = reader.ReadUInt32();

        var startCharCode = new uint[numGroups];
        var endCharCode = new uint[numGroups];
        var startGlyphId = new uint[numGroups];
        for (var i = 0; i < numGroups; i++)
        {
            startCharCode[i] = reader.ReadUInt32();
            endCharCode[i] = reader.ReadUInt32();
            startGlyphId[i] = reader.ReadUInt32();
        }

        return new Format12Subtable(startCharCode, endCharCode, startGlyphId);
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
