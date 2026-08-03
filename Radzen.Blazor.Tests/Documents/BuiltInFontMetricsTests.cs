#nullable enable
using System;
using System.Collections.Generic;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Pdf.Fonts;

namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;

public class BuiltInFontMetricsTests
{
    private static Font MakeFont(string name, bool bold = false, bool italic = false)
        => new() { Family = name, Bold = bold, Italic = italic };

    private static string GlyphNameFor(char c) => c switch
    {
        ' ' => "space",
        '!' => "exclam",
        '0' => "zero",
        '1' => "one",
        '2' => "two",
        '3' => "three",
        'é' => "eacute",
        '“' => "quotedblleft",
        '”' => "quotedblright",
        _ => c.ToString(),
    };

    private static double ExpectedSum(AfmReference afm, string text)
    {
        double sum = 0;
        foreach (var c in text)
        {
            sum += afm.WidthByName[GlyphNameFor(c)];
        }

        return sum;
    }

    [Theory]
    [InlineData("Helvetica", "Helvetica")]
    [InlineData("Helvetica-Bold", "Helvetica-Bold")]
    [InlineData("Times-Roman", "Times-Roman")]
    [InlineData("Courier", "Courier")]
    public void MeasureString_AsciiSampleEqualsAfmWidthSum(string psName, string afmFile)
    {
        var afm = AfmReference.Load(afmFile);
        var metrics = BuiltInFontMetrics.Resolve(MakeFont(psName));
        Assert.NotNull(metrics);

        const string sample = "Hello World! 123";
        Assert.Equal(ExpectedSum(afm, sample), metrics!.MeasureString(sample, 1000));
    }

    [Fact]
    public void MeasureString_Latin1SampleEqualsAfmWidthSum()
    {
        var afm = AfmReference.Load("Times-Roman");
        var metrics = BuiltInFontMetrics.Resolve(MakeFont("Times-Roman"));
        Assert.NotNull(metrics);

        const string sample = "résumé “ok”";
        Assert.Equal(ExpectedSum(afm, sample), metrics!.MeasureString(sample, 1000));
    }

    [Fact]
    public void MeasureString_ScalesLinearlyWithFontSize()
    {
        var afm = AfmReference.Load("Helvetica");
        var metrics = BuiltInFontMetrics.Resolve(MakeFont("Helvetica"))!;

        const string sample = "Hello World! 123";
        var expected = ExpectedSum(afm, sample) * 12.0 / 1000.0;
        Assert.Equal(expected, metrics.MeasureString(sample, 12), 9);
    }

    [Fact]
    public void MeasureString_ThrowsOnUnencodableChars()
    {
        var metrics = BuiltInFontMetrics.Resolve(MakeFont("Helvetica"))!;

        var error = Assert.Throws<InvalidOperationException>(() => metrics.MeasureString("AБB", 1000));

        Assert.Contains("U+0411", error.Message, StringComparison.Ordinal);
        Assert.Contains("Helvetica", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void GetWidth_KnownCharacterMatchesAfm()
    {
        var afm = AfmReference.Load("Helvetica");
        var metrics = BuiltInFontMetrics.Resolve(MakeFont("Helvetica"))!;

        Assert.True(metrics.TryGetWidth('A', out var widthA));
        Assert.Equal((double)afm.WidthByName["A"], widthA);
        Assert.True(metrics.TryGetWidth('é', out var widthE));
        Assert.Equal((double)afm.WidthByName["eacute"], widthE);
    }

    [Theory]
    [InlineData(129)]
    [InlineData(0)]
    public void GetWidth_CharacterWithoutMetricsIsMissing(int codepoint)
    {
        var metrics = BuiltInFontMetrics.Resolve(MakeFont("Helvetica"))!;
        Assert.False(metrics.TryGetWidth(codepoint, out _));
    }

    public static IEnumerable<object[]> StyleMatrix() => new List<object[]>
    {
        new object[] { "Helvetica", false, false, "Helvetica" },
        new object[] { "Helvetica", true, false, "Helvetica-Bold" },
        new object[] { "Helvetica", false, true, "Helvetica-Oblique" },
        new object[] { "Helvetica", true, true, "Helvetica-BoldOblique" },
        new object[] { "Times", false, false, "Times-Roman" },
        new object[] { "Times", true, false, "Times-Bold" },
        new object[] { "Times", false, true, "Times-Italic" },
        new object[] { "Times", true, true, "Times-BoldItalic" },
        new object[] { "Courier", false, false, "Courier" },
        new object[] { "Courier", true, false, "Courier-Bold" },
        new object[] { "Courier", false, true, "Courier-Oblique" },
        new object[] { "Courier", true, true, "Courier-BoldOblique" },
    };

    [Theory]
    [MemberData(nameof(StyleMatrix))]
    public void Resolve_MapsStyleToPostScriptName(string family, bool bold, bool italic, string expected)
    {
        var metrics = BuiltInFontMetrics.Resolve(MakeFont(family, bold, italic));
        Assert.NotNull(metrics);
        Assert.Equal(expected, StandardFonts.PostScriptName(metrics!.Face()));
    }

    [Fact]
    public void Resolve_IsCaseInsensitiveForFamilyNames()
    {
        var metrics = BuiltInFontMetrics.Resolve(MakeFont("helvetica", bold: true));
        Assert.NotNull(metrics);
        Assert.Equal("Helvetica-Bold", StandardFonts.PostScriptName(metrics!.Face()));
    }

    [Theory]
    [InlineData("Times-Bold")]
    [InlineData("Courier-Oblique")]
    [InlineData("Helvetica-BoldOblique")]
    public void Resolve_AcceptsDirectPostScriptNames(string psName)
    {
        var metrics = BuiltInFontMetrics.Resolve(MakeFont(psName));
        Assert.NotNull(metrics);
        Assert.Equal(psName, StandardFonts.PostScriptName(metrics!.Face()));
    }

    [Theory]
    [InlineData("Arial")]
    [InlineData("Times New Roman")]
    [InlineData("Comic Sans")]
    [InlineData("")]
    public void Resolve_ReturnsNullForUnknownFamily(string name)
    {
        Assert.Null(BuiltInFontMetrics.Resolve(MakeFont(name)));
    }

    [Theory]
    [InlineData("Symbol", "Symbol")]
    [InlineData("ZapfDingbats", "ZapfDingbats")]
    public void Resolve_SymbolicFontsIgnoreStyleFlags(string family, string expected)
    {
        var metrics = BuiltInFontMetrics.Resolve(MakeFont(family, bold: true, italic: true));
        Assert.NotNull(metrics);
        Assert.Equal(expected, StandardFonts.PostScriptName(metrics!.Face()));
    }

    [Theory]
    [InlineData("Symbol", 0x61)]
    [InlineData("ZapfDingbats", 0x61)]
    public void GetWidth_SymbolicFontsUseNativeCharacterMap(string family, int codepoint)
    {
        var afm = AfmReference.Load(family);
        var metrics = BuiltInFontMetrics.Resolve(MakeFont(family))!;
        Assert.True(metrics.TryGetWidth(codepoint, out var width));
        Assert.Equal((double)afm.WidthByCode[codepoint], width);
    }

    [Theory]
    [InlineData("Helvetica")]
    [InlineData("Times-Roman")]
    [InlineData("Courier")]
    public void FontMetrics_MatchAfmHeader(string afmFile)
    {
        var afm = AfmReference.Load(afmFile);
        var metrics = BuiltInFontMetrics.Resolve(MakeFont(afmFile))!;

        Assert.Equal(afm.CapHeight, metrics.CapHeight);
        Assert.Equal(afm.XHeight, metrics.XHeight);
        Assert.Equal(afm.Ascender, metrics.Ascender);
        Assert.Equal(afm.Descender, metrics.Descender);
        Assert.Equal(afm.ItalicAngle, metrics.ItalicAngle);
        Assert.Equal(afm.IsFixedPitch, metrics.IsFixedPitch);
        Assert.Equal(afm.BBoxLeft, metrics.BBoxLeft);
        Assert.Equal(afm.BBoxBottom, metrics.BBoxBottom);
        Assert.Equal(afm.BBoxRight, metrics.BBoxRight);
        Assert.Equal(afm.BBoxTop, metrics.BBoxTop);
    }

    [Fact]
    public void ItalicAngle_IsNonZeroForObliqueVariant()
    {
        var afm = AfmReference.Load("Helvetica-Oblique");
        var metrics = BuiltInFontMetrics.Resolve(MakeFont("Helvetica", italic: true))!;
        Assert.Equal(afm.ItalicAngle, metrics.ItalicAngle);
        Assert.NotEqual(0d, metrics.ItalicAngle);
    }
}
