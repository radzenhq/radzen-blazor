#nullable enable
using System;
using Xunit;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Blazor.Pdf.Tests;

// All hardcoded expectations derived from the exact fixture files via
// `python3 - <<EOF` scripts using fontTools.ttLib.TTFont / SFNTReader (fontTools 4.60.2).
// Actual numeric properties are cast to int/double at the assert site so the tests do
// not depend on the exact integer width the implementer picks (ushort vs short vs int).
public class SfntParserTests
{
    private static SfntFont LiberationSansRegular()
        => SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf"));

    [Fact]
    public void LiberationSansRegular_HeadAndNames()
    {
        var font = LiberationSansRegular();

        Assert.Equal(2048, (int)font.UnitsPerEm);
        Assert.Equal(2620, (int)font.GlyphCount);
        Assert.Equal("Liberation Sans", font.FamilyName);
        Assert.Equal("Regular", font.SubfamilyName);
        Assert.Equal("LiberationSans", font.PostScriptName);
    }

    [Fact]
    public void LiberationSansRegular_Metrics()
    {
        var font = LiberationSansRegular();

        Assert.Equal(1854, (int)font.Ascent);
        Assert.Equal(-434, (int)font.Descent);
        Assert.Equal(67, (int)font.LineGap);
        Assert.Equal(1409, (int)font.CapHeight);
        Assert.Equal(0d, (double)font.ItalicAngle);
    }

    [Fact]
    public void LiberationSansRegular_IsTrueTypeNotCff()
    {
        Assert.False(LiberationSansRegular().IsCff);
    }

    [Fact]
    public void LiberationSansRegular_StyleFlagsFalse()
    {
        var font = LiberationSansRegular();

        Assert.False(font.Bold);
        Assert.False(font.Italic);
    }

    [Fact]
    public void LiberationSansBold_BoldFlagTrue()
    {
        var font = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Bold.ttf"));

        Assert.True(font.Bold);
        Assert.False(font.Italic);
        Assert.Equal("Bold", font.SubfamilyName);
    }

    [Fact]
    public void LiberationSansBoldItalic_BothFlagsTrue()
    {
        var font = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-BoldItalic.ttf"));

        Assert.True(font.Bold);
        Assert.True(font.Italic);
        Assert.Equal(-12d, (double)font.ItalicAngle);
    }

    [Fact]
    public void LiberationSerif_FamilyNameDiffers()
    {
        var font = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/LiberationSerif-Regular.ttf"));

        Assert.Equal("Liberation Serif", font.FamilyName);
        Assert.False(font.Bold);
        Assert.False(font.Italic);
    }

    [Theory]
    [InlineData('A', 36)]
    [InlineData('z', 93)]
    [InlineData('0', 19)]
    [InlineData(' ', 3)]
    [InlineData('W', 58)]
    [InlineData(0x0411, 962)]  // Cyrillic capital Be
    [InlineData(0x044F, 1024)] // Cyrillic small ya
    public void LiberationSansRegular_CmapFormat4_GlyphIds(int codepoint, int expectedGid)
    {
        Assert.Equal(expectedGid, (int)LiberationSansRegular().GetGlyphId(codepoint));
    }

    [Fact]
    public void LiberationSansRegular_UnmappedCodepointReturnsNotdef()
    {
        // U+2603 SNOWMAN is not in Liberation Sans (verified via fonttools getBestCmap).
        Assert.Equal(0, (int)LiberationSansRegular().GetGlyphId(0x2603));
    }

    [Theory]
    [InlineData(36, 1366)]  // 'A'
    [InlineData(3, 569)]    // space
    [InlineData(58, 1933)]  // 'W'
    public void LiberationSansRegular_AdvanceWidths(int gid, int expected)
    {
        Assert.Equal(expected, (int)LiberationSansRegular().GetAdvanceWidth((ushort)gid));
    }

    private static SfntFont NotoSubset()
        => SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf"));

    [Fact]
    public void NotoSubset_IsCffOpenType()
    {
        var font = NotoSubset();

        Assert.True(font.IsCff);
        Assert.Equal(1000, (int)font.UnitsPerEm);
        Assert.Equal(658, (int)font.GlyphCount);
        Assert.Equal("Noto Sans SC", font.FamilyName);
    }

    [Theory]
    [InlineData(0x53D1, 401)] // 发
    [InlineData(0x7968, 418)] // 票
    [InlineData('A', 34)]
    [InlineData(0x0431, 223)] // Cyrillic small be
    [InlineData(' ', 1)]
    public void NotoSubset_GlyphIds(int codepoint, int expectedGid)
    {
        Assert.Equal(expectedGid, (int)NotoSubset().GetGlyphId(codepoint));
    }

    [Theory]
    [InlineData(401, 1000)] // 发
    [InlineData(418, 1000)] // 票
    [InlineData(34, 608)]   // 'A'
    [InlineData(223, 608)]  // Cyrillic be
    [InlineData(1, 224)]    // space
    public void NotoSubset_AdvanceWidths(int gid, int expected)
    {
        Assert.Equal(expected, (int)NotoSubset().GetAdvanceWidth((ushort)gid));
    }

    [Fact]
    public void NotoSubset_GlyphBeyondNumberOfHMetricsReusesLastAdvance()
    {
        // numberOfHMetrics=655, numGlyphs=658; glyph 657 has no explicit metric
        // and must reuse the last hmtx advance (1000) per the OpenType spec.
        Assert.Equal(1000, (int)NotoSubset().GetAdvanceWidth(657));
    }

    [Theory]
    [InlineData("GSUB", 5910)]
    [InlineData("GPOS", 14804)]
    [InlineData("BASE", 210)]
    public void NotoSubset_TryGetTable_ReturnsRawBytesMatchingDirectoryLength(string tag, int length)
    {
        Assert.True(NotoSubset().TryGetTable(tag, out var data));
        Assert.Equal(length, data.Length);
    }

    [Fact]
    public void NotoSubset_TryGetTable_MissingTableReturnsFalse()
    {
        // GDEF is not present in the subset (verified via fonttools SFNTReader).
        Assert.False(NotoSubset().TryGetTable("GDEF", out var data));
        Assert.Null(data);
    }

    [Fact]
    public void Parse_TruncatedFile_Throws()
    {
        var full = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf");
        var truncated = new byte[4];
        Array.Copy(full, truncated, 4);

        Assert.ThrowsAny<Exception>(() => SfntFont.Parse(truncated));
    }

    [Fact]
    public void Parse_BadMagic_Throws()
    {
        Assert.ThrowsAny<Exception>(() => SfntFont.Parse(new byte[16]));
    }
}
