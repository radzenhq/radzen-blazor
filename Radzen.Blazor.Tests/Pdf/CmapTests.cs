#nullable enable
using System.Collections.Generic;
using Xunit;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Blazor.Pdf.Tests;

public class CmapTests
{
    private static byte[] BuildFormat12Cmap()
    {
        var bytes = new List<byte>();

        void U16(int v) { bytes.Add((byte)(v >> 8)); bytes.Add((byte)v); }
        void U32(long v)
        {
            bytes.Add((byte)(v >> 24));
            bytes.Add((byte)(v >> 16));
            bytes.Add((byte)(v >> 8));
            bytes.Add((byte)v);
        }

        U16(0);
        U16(1);
        U16(3);
        U16(10);
        U32(12);

        const int numGroups = 2;
        U16(12);
        U16(0);
        U32(16 + numGroups * 12);
        U32(0);
        U32(numGroups);
        U32(0x1F600); U32(0x1F600); U32(5);
        U32(0x1F601); U32(0x1F603); U32(6);

        return bytes.ToArray();
    }

    [Fact]
    public void Format12_SingleMapping()
    {
        var mapper = Cmap.Parse(BuildFormat12Cmap());
        Assert.Equal(5, (int)mapper.GetGlyphId(0x1F600));
    }

    [Fact]
    public void Format12_SequentialRange()
    {
        var mapper = Cmap.Parse(BuildFormat12Cmap());
        Assert.Equal(6, (int)mapper.GetGlyphId(0x1F601));
        Assert.Equal(7, (int)mapper.GetGlyphId(0x1F602));
        Assert.Equal(8, (int)mapper.GetGlyphId(0x1F603));
    }

    [Fact]
    public void Format12_UnmappedReturnsNotdef()
    {
        var mapper = Cmap.Parse(BuildFormat12Cmap());
        Assert.Equal(0, (int)mapper.GetGlyphId(0x1F610));
        Assert.Equal(0, (int)mapper.GetGlyphId('A'));
    }
}
