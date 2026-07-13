using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Fonts.Sfnt;

// Parses the legacy 'kern' table (OpenType/Windows version 0) horizontal format-0
// subtables into pair advance adjustments in font design units, keyed by
// (leftGlyphId << 16) | rightGlyphId. The Apple version-1.0 header (uint32 0x00010000)
// and non-format-0 subtables are ignored, yielding no pairs from them.
internal static class KernTable
{
    public static Dictionary<int, int> Parse(byte[] data)
    {
        var map = new Dictionary<int, int>();
        var reader = new SfntReader(data);
        if (reader.Length < 4)
        {
            return map;
        }

        var version = reader.ReadUInt16At(0);
        if (version != 0)
        {
            return map;
        }

        var nTables = reader.ReadUInt16();
        var pos = 4;
        for (var t = 0; t < nTables && pos + 6 <= reader.Length; t++)
        {
            var length = reader.ReadUInt16At(pos + 2);
            var coverage = reader.ReadUInt16At(pos + 4);
            var format = (coverage >> 8) & 0xFF;
            var horizontal = (coverage & 0x0001) != 0;
            var minimum = (coverage & 0x0002) != 0;
            if (format == 0 && horizontal && !minimum)
            {
                // Bound the pair loop by this subtable, not the whole table: an overstated
                // nPairs must not read the following subtable's bytes as kern pairs.
                var end = length > 0 ? Math.Min(reader.Length, pos + length) : reader.Length;
                ParseFormat0(ref reader, pos + 6, end, map);
            }

            if (length == 0)
            {
                break;
            }

            pos += length;
        }

        return map;
    }

    private static void ParseFormat0(ref SfntReader reader, int offset, int end, Dictionary<int, int> map)
    {
        if (offset + 8 > end)
        {
            return;
        }

        var nPairs = reader.ReadUInt16At(offset);
        var maxPairs = (end - (offset + 8)) / 6;
        if (nPairs > maxPairs)
        {
            nPairs = (ushort)maxPairs;
        }

        var p = offset + 8; // skip searchRange, entrySelector, rangeShift
        for (var i = 0; i < nPairs && p + 6 <= end; i++)
        {
            var left = reader.ReadUInt16At(p);
            var right = reader.ReadUInt16At(p + 2);
            var value = reader.ReadInt16At(p + 4);
            map[(left << 16) | right] = value;
            p += 6;
        }
    }
}
