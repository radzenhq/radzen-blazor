#nullable enable
using System;
using System.IO;
using Xunit;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Radzen.Documents.Pdf.Fonts.Cff;

namespace Radzen.Blazor.Pdf.Tests;

// Every expectation is derived from the exact fixture NotoSansSC-Subset.otf with
// fontTools 4.60.2 against font["CFF "].cff: fontNames[0], getGlyphOrder length,
// ROS, charset (cidNNNNN names -> CID), FDSelect (format 3), len(FDArray), and
// advance widths from the Type 2 charstring width machinery (which agree with hmtx).
// This is a CID-keyed CFF (ROS Adobe-Identity-0), 658 glyphs, 11 FDArray entries.
//
// Coverage note: the only CFF fixture is CID-keyed, so the non-CID (name-keyed)
// parsing path is NOT exercised here. GetFd's non-CID contract is pinned by a
// negative test below (throws when the font is CID-keyed is not applicable, so we
// instead pin that GetFd returns a valid FD for this CID font). INDEX/DICT
// primitives are unit-tested on hand-built bytes in CffIndexTests.
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

    // charset[gid] -> CID for the CID-keyed font (name cidNNNNN -> NNNNN; .notdef -> 0).
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

    // Advance widths come from the Type 2 charstring width operand (defaultWidthX /
    // nominalWidthX in the per-FD Private DICT). Hardcoded fonttools numbers.
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

    // Cross-check: CFF charstring widths must agree with the sfnt hmtx for the same gid.
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

    // FDSelect format 3 -> FD index per glyph.
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
        // First byte is the CFF major version; 0x00 is not a valid CFF header.
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
