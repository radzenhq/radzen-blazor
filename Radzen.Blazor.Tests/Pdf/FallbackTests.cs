#nullable enable
using System;
using System.IO;
using Xunit;
using Radzen.Documents.Pdf;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

public class FallbackTests
{
    private const char Cjk = '中';

    private static SfntFont Sans()
        => SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf"));

    private static SfntFont Noto()
        => SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf"));

    private static FontCollection Registered()
    {
        var fonts = new FontCollection();
        fonts.Register("Liberation Sans", new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        fonts.Register("Noto Sans SC", new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf")));
        return fonts;
    }

    [Fact]
    public void Shape_MixedString_PrimaryAndFallbackGlyphs()
    {
        var fonts = Registered();
        fonts.SetFallback("Liberation Sans", "Noto Sans SC");
        var shaper = new SimpleShaper(fonts);
        var font = new Font { Family = "Liberation Sans", Size = 12 };

        var glyphs = shaper.Shape(new[] { 'A', Cjk }, font, out _);

        Assert.Equal(2, glyphs.Count);

        Assert.Equal(Sans().GetGlyphId('A'), glyphs[0].GlyphId);

        var notoGid = Noto().GetGlyphId(Cjk);
        Assert.NotEqual(0, notoGid);
        Assert.Equal(notoGid, glyphs[1].GlyphId);
    }

    [Fact]
    public void Shape_FallbackGlyphAdvance_UsesFallbackFaceMetrics()
    {
        var fonts = Registered();
        fonts.SetFallback("Liberation Sans", "Noto Sans SC");
        var shaper = new SimpleShaper(fonts);
        var font = new Font { Family = "Liberation Sans", Size = 12 };

        var glyphs = shaper.Shape(new[] { 'A', Cjk }, font, out _);

        var noto = Noto();
        var expected = noto.GetAdvanceWidth(noto.GetGlyphId(Cjk)) * 12.0 / noto.UnitsPerEm;
        Assert.Equal(expected, glyphs[1].Advance, 10);
    }

    [Fact]
    public void MeasureText_CjkFallsBackToNoto()
    {
        var fonts = Registered();
        fonts.SetFallback("Liberation Sans", "Noto Sans SC");
        var font = new Font { Family = "Liberation Sans", Size = 12 };

        var noto = Noto();
        var expected = noto.GetAdvanceWidth(noto.GetGlyphId(Cjk)) * 12.0 / noto.UnitsPerEm;
        Assert.Equal(expected, fonts.MeasureText(Cjk.ToString(), font), 10);
    }

    [Fact]
    public void PureAscii_UsesPrimaryNotFallback()
    {
        var fonts = Registered();
        fonts.SetFallback("Liberation Sans", "Noto Sans SC");
        var shaper = new SimpleShaper(fonts);
        var font = new Font { Family = "Liberation Sans", Size = 12 };

        var glyphs = shaper.Shape("A", font, out _);

        Assert.Equal(Sans().GetGlyphId('A'), glyphs[0].GlyphId);
        Assert.NotEqual(Noto().GetGlyphId('A'), glyphs[0].GlyphId);

        var sans = Sans();
        var expected = sans.GetAdvanceWidth(sans.GetGlyphId('A')) * 12.0 / sans.UnitsPerEm;
        Assert.Equal(expected, fonts.MeasureText("A", font), 10);
    }

    [Fact]
    public void Shape_WithoutFallback_MissingGlyphIsNotdef()
    {
        var fonts = Registered();
        var shaper = new SimpleShaper(fonts);
        var font = new Font { Family = "Liberation Sans", Size = 12 };

        var glyphs = shaper.Shape(new[] { 'A', Cjk }, font, out _);

        Assert.Equal(Sans().GetGlyphId('A'), glyphs[0].GlyphId);
        Assert.Equal(0, glyphs[1].GlyphId);
    }
}
