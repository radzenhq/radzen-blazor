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
                ParseFormat0(ref reader, pos + 6, map);
            }

            if (length == 0)
            {
                break;
            }

            pos += length;
        }

        return map;
    }

    private static void ParseFormat0(ref SfntReader reader, int offset, Dictionary<int, int> map)
    {
        if (offset + 8 > reader.Length)
        {
            return;
        }

        var nPairs = reader.ReadUInt16At(offset);
        var p = offset + 8; // skip searchRange, entrySelector, rangeShift
        for (var i = 0; i < nPairs && p + 6 <= reader.Length; i++)
        {
            var left = reader.ReadUInt16At(p);
            var right = reader.ReadUInt16At(p + 2);
            var value = reader.ReadInt16At(p + 4);
            map[(left << 16) | right] = value;
            p += 6;
        }
    }
}
