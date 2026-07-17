#nullable enable
using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class KernTableHardeningTests
{
    [Fact]
    public void OverstatedNPairs_DoesNotBleedIntoNextSubtable()
    {
        var kern = new List<byte>();
        void U16(int v) { kern.Add((byte)(v >> 8)); kern.Add((byte)v); }

        U16(0);
        U16(2);

        U16(0);
        U16(20);
        U16(0x0001);
        U16(3);
        U16(0); U16(0); U16(0);
        U16(10); U16(20); U16(0xFFCE);

        U16(0);
        U16(20);
        U16(0x0001);
        U16(1);
        U16(0); U16(0); U16(0);
        U16(30); U16(40); U16(0xFFC4);

        var map = KernTable.Parse(kern.ToArray());

        Assert.Equal(-50, map[(10 << 16) | 20]);
        Assert.Equal(-60, map[(30 << 16) | 40]);
        Assert.False(map.ContainsKey((0 << 16) | 20), "subtable-2 header bytes must not be read as a kern pair");
        Assert.Equal(2, map.Count);
    }
}
