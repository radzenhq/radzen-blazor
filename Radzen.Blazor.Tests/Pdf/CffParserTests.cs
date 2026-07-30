#nullable enable
using System;
using System.IO;
using Xunit;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents.Pdf.Fonts.Cff;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class CffParserTests
{
    private static CffFont ParseCff()
    {
        var sfnt = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf"));
        Assert.True(sfnt.TryGetTable("CFF ", out var cffTable));
        return CffFont.Parse(cffTable);
    }

    private static (CffFont Cff, SfntFont Sfnt) ParseBoth()
    {
        var sfnt = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf"));
        Assert.True(sfnt.TryGetTable("CFF ", out var cffTable));
        return (CffFont.Parse(cffTable), sfnt);
    }

    [Fact]
    public void Parse_ReportsNameGlyphCountAndCidKeyed()
    {
        var cff = ParseCff();

        Assert.Equal("NotoSansSC-Regular", cff.FontName);
        Assert.Equal(658, cff.GlyphCount);
        Assert.True(cff.IsCidKeyed);
    }

    [Fact]
    public void Parse_ExposesRegistryOrderingSupplement()
    {
        var cff = ParseCff();

        Assert.Equal("Adobe", cff.Registry);
        Assert.Equal("Identity", cff.Ordering);
        Assert.Equal(0, cff.Supplement);
    }

    [Fact]
    public void Charset_HasOneEntryPerGlyph()
    {
        var cff = ParseCff();

        Assert.Equal(658, cff.Charset.Length);
        Assert.Equal(0, cff.Charset[0]);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(34, 34)]
    [InlineData(66, 66)]
    [InlineData(190, 307)]
    [InlineData(223, 340)]
    [InlineData(300, 2341)]
    [InlineData(401, 11872)]
    [InlineData(418, 28904)]
    [InlineData(657, 65456)]
    public void Charset_MapsGlyphToCid(int glyphIndex, int expectedCid)
    {
        var cff = ParseCff();

        Assert.Equal(expectedCid, cff.Charset[glyphIndex]);
    }

    [Theory]
    [InlineData(0, 1000)]
    [InlineData(1, 224)]
    [InlineData(34, 608)]
    [InlineData(66, 563)]
    [InlineData(190, 608)]
    [InlineData(223, 608)]
    [InlineData(300, 1000)]
    [InlineData(401, 1000)]
    [InlineData(418, 1000)]
    [InlineData(500, 1000)]
    [InlineData(600, 500)]
    [InlineData(657, 1000)]
    public void GetAdvanceWidth_FromCharstring(int glyphIndex, int expected)
    {
        var cff = ParseCff();

        Assert.Equal(expected, cff.GetAdvanceWidth(glyphIndex));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(34)]
    [InlineData(66)]
    [InlineData(190)]
    [InlineData(223)]
    [InlineData(401)]
    [InlineData(600)]
    [InlineData(657)]
    public void GetAdvanceWidth_MatchesSfntHmtx(int glyphIndex)
    {
        var (cff, sfnt) = ParseBoth();

        Assert.Equal((int)sfnt.GetAdvanceWidth((ushort)glyphIndex), cff.GetAdvanceWidth(glyphIndex));
    }

    [Fact]
    public void FdCount_MatchesFdArrayLength()
    {
        var cff = ParseCff();

        Assert.Equal(11, cff.FdCount);
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(1, 8)]
    [InlineData(34, 8)]
    [InlineData(66, 8)]
    [InlineData(190, 8)]
    [InlineData(223, 8)]
    [InlineData(300, 2)]
    [InlineData(401, 7)]
    [InlineData(418, 7)]
    [InlineData(500, 0)]
    [InlineData(600, 4)]
    [InlineData(657, 2)]
    public void GetFd_MapsGlyphToFdIndex(int glyphIndex, int expectedFd)
    {
        var cff = ParseCff();

        Assert.Equal(expectedFd, cff.GetFd(glyphIndex));
    }

    [Fact]
    public void GetFd_IsWithinFdArrayRange()
    {
        var cff = ParseCff();

        for (var gid = 0; gid < cff.GlyphCount; gid++)
        {
            var fd = cff.GetFd(gid);
            Assert.InRange(fd, 0, cff.FdCount - 1);
        }
    }

    [Fact]
    public void Parse_GarbageBytes_Throws()
    {
        var garbage = new byte[] { 0x00, 0x01, 0x02, 0x03, 0x63, 0x63, 0x63, 0x63 };

        Assert.Throws<InvalidDataException>(() => CffFont.Parse(garbage));
    }

    [Fact]
    public void Parse_TruncatedCff_Throws()
    {
        var sfnt = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf"));
        Assert.True(sfnt.TryGetTable("CFF ", out var cffTable));
        var truncated = new byte[32];
        Array.Copy(cffTable, truncated, 32);

        Assert.ThrowsAny<Exception>(() => CffFont.Parse(truncated));
    }
}
