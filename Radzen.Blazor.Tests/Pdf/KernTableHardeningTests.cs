#nullable enable
using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// The legacy 'kern' format-0 pair loop must be bounded by its own subtable, not by the
// whole table: an overstated nPairs must not read the following subtable's bytes as pairs.
public class KernTableHardeningTests
{
    [Fact]
    public void OverstatedNPairs_DoesNotBleedIntoNextSubtable()
    {
        var kern = new List<byte>();
        void U16(int v) { kern.Add((byte)(v >> 8)); kern.Add((byte)v); }

        U16(0); // version 0
        U16(2); // nTables

        // Subtable 1: length 20 (6-byte header + 8-byte format-0 header + one 6-byte pair),
        // but nPairs claims 3 - the extra two would fall into subtable 2 if unbounded.
        U16(0);  // subtable version
        U16(20); // length
        U16(0x0001); // coverage: format 0, horizontal
        U16(3);  // nPairs (overstated)
        U16(0); U16(0); U16(0); // searchRange, entrySelector, rangeShift
        U16(10); U16(20); U16(0xFFCE); // pair (10,20) = -50

        // Subtable 2: one legitimate pair.
        U16(0);  // subtable version
        U16(20); // length
        U16(0x0001); // coverage
        U16(1);  // nPairs
        U16(0); U16(0); U16(0);
        U16(30); U16(40); U16(0xFFC4); // pair (30,40) = -60

        var map = KernTable.Parse(kern.ToArray());

        Assert.Equal(-50, map[(10 << 16) | 20]);
        Assert.Equal(-60, map[(30 << 16) | 40]);
        Assert.False(map.ContainsKey((0 << 16) | 20), "subtable-2 header bytes must not be read as a kern pair");
        Assert.Equal(2, map.Count);
    }
}
