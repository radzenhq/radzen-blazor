#nullable enable
using System;
using System.IO;

namespace Radzen.Documents.Pdf.Fonts.Sfnt;

// Character-to-glyph mapper built from a whole cmap table. Supports formats 4
// and 12; when both are present the format 12 subtable wins (full Unicode range).
internal sealed class Cmap
{
    private readonly ICmapSubtable subtable;

    private Cmap(ICmapSubtable subtable) => this.subtable = subtable;

    public ushort GetGlyphId(int codepoint) => subtable.GetGlyphId(codepoint);

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
                }
            }

            reader.Position = next;
        }

        if (best == null)
        {
            throw new InvalidDataException("No supported cmap subtable (format 4 or 12) found.");
        }

        return new Cmap(best);
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

        var c = (ushort)codepoint;
        for (var i = 0; i < endCode.Length; i++)
        {
            if (c > endCode[i])
            {
                continue;
            }

            if (c < startCode[i])
            {
                return 0;
            }

            if (idRangeOffset[i] == 0)
            {
                return (ushort)((c + idDelta[i]) & 0xFFFF);
            }

            var glyphIndexOffset = idRangeOffsetBase + (i * 2) + idRangeOffset[i] + ((c - startCode[i]) * 2);
            var glyph = new SfntReader(data).ReadUInt16At(glyphIndexOffset);
            if (glyph == 0)
            {
                return 0;
            }

            return (ushort)((glyph + idDelta[i]) & 0xFFFF);
        }

        return 0;
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
