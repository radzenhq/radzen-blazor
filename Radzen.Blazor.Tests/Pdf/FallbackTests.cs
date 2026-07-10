#nullable enable
using System;
using System.IO;
using Xunit;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Blazor.Pdf.Tests;

// Contract pinned for FontCollection.SetFallback(params string[] families) (F5).
//
// Pinned semantics: SetFallback declares an ordered fallback chain among REGISTERED
// families. When the primary font (Font.Name) lacks a glyph for a codepoint (cmap gid 0),
// the collection walks the fallback chain and uses the first registered family that maps
// the codepoint to a non-notdef glyph. Both MeasureText and SimpleShaper honor it.
//
// Fixtures: Liberation Sans has Latin/Cyrillic but NO CJK; NotoSansSC-Subset has CJK
// (plus Latin/Cyrillic). Verified via fonttools: U+4E2D is absent from Liberation Sans
// and present in Noto (gid 395, advance 1000, unitsPerEm 1000); Liberation 'A' gid 36
// differs from Noto 'A' gid 34, so a glyph id uniquely identifies its source face.
public class FallbackTests
{
    private const char Cjk = '中'; // CJK ideograph absent from Liberation Sans

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
        var font = new Font { Name = "Liberation Sans", Size = 12 };

        var run = shaper.Shape(new[] { 'A', Cjk }, font, FlowDirection.LeftToRight, WritingMode.HorizontalTopToBottom);

        Assert.Equal(2, run.Glyphs.Count);

        // 'A' resolved by the primary (Liberation gid 36, not Noto's 34).
        Assert.Equal(Sans().GetGlyphId('A'), run.Glyphs[0].GlyphId);

        // CJK resolved via the fallback: non-notdef and equal to Noto's glyph id.
        var notoGid = Noto().GetGlyphId(Cjk);
        Assert.NotEqual(0, notoGid);
        Assert.Equal(notoGid, run.Glyphs[1].GlyphId);
    }

    [Fact]
    public void Shape_FallbackGlyphAdvance_UsesFallbackFaceMetrics()
    {
        var fonts = Registered();
        fonts.SetFallback("Liberation Sans", "Noto Sans SC");
        var shaper = new SimpleShaper(fonts);
        var font = new Font { Name = "Liberation Sans", Size = 12 };

        var run = shaper.Shape(new[] { 'A', Cjk }, font, FlowDirection.LeftToRight, WritingMode.HorizontalTopToBottom);

        var noto = Noto();
        var expected = noto.GetAdvanceWidth(noto.GetGlyphId(Cjk)) * 12.0 / noto.UnitsPerEm;
        Assert.Equal(expected, run.Glyphs[1].Advance, 10);
    }

    [Fact]
    public void MeasureText_CjkFallsBackToNoto()
    {
        var fonts = Registered();
        fonts.SetFallback("Liberation Sans", "Noto Sans SC");
        var font = new Font { Name = "Liberation Sans", Size = 12 };

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
        var font = new Font { Name = "Liberation Sans", Size = 12 };

        var run = shaper.Shape("A", font, FlowDirection.LeftToRight, WritingMode.HorizontalTopToBottom);

        Assert.Equal(Sans().GetGlyphId('A'), run.Glyphs[0].GlyphId);
        Assert.NotEqual(Noto().GetGlyphId('A'), run.Glyphs[0].GlyphId);

        var sans = Sans();
        var expected = sans.GetAdvanceWidth(sans.GetGlyphId('A')) * 12.0 / sans.UnitsPerEm;
        Assert.Equal(expected, fonts.MeasureText("A", font), 10);
    }

    [Fact]
    public void Shape_WithoutFallback_MissingGlyphIsNotdef()
    {
        var fonts = Registered(); // no SetFallback
        var shaper = new SimpleShaper(fonts);
        var font = new Font { Name = "Liberation Sans", Size = 12 };

        var run = shaper.Shape(new[] { 'A', Cjk }, font, FlowDirection.LeftToRight, WritingMode.HorizontalTopToBottom);

        Assert.Equal(Sans().GetGlyphId('A'), run.Glyphs[0].GlyphId);
        Assert.Equal(0, run.Glyphs[1].GlyphId); // notdef: no fallback configured
    }
}
