#nullable enable
using System;
using System.IO;
using Xunit;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Blazor.Pdf.Tests;

// Contract pinned for the internal text-shaping helper SimpleShaper (the ITextShaper /
// GlyphRun / PositionedGlyph public seam was dropped; nothing implements a complex shaper).
//
// Pinned shape:
//  - internal SimpleShaper(FontCollection fonts, bool enableKerning = false)
//  - List<PositionedGlyph> Shape(ReadOnlySpan<char> text, Font font, out double advance);
//    identity codepoint->glyph via the resolved font's cmap; per-glyph Advance = hmtx
//    advance * font.Size / UnitsPerEm; advance out = sum of per-glyph advances.
//  - PositionedGlyph { ushort GlyphId; double Advance; int Cluster; SfntFont Face; } (internal)
//
// KERN FINDING: Liberation Sans DOES ship a legacy format-0 'kern' table (908 pairs; A/V =
// -152 units). The default helper pins NO kerning: MeasureText and the shaped advance are
// both pure hmtx sums, so they stay equal (asserted below). Legacy 'kern' is applied only
// when enableKerning is set, so default byte output is unchanged.
public class SimpleShaperTests
{
    private static FontCollection LiberationSans()
    {
        var fonts = new FontCollection();
        fonts.Register("Liberation Sans", new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        return fonts;
    }

    private static SfntFont SansFace()
        => SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf"));

    private static double Advance(SfntFont face, char c, double size)
        => face.GetAdvanceWidth(face.GetGlyphId(c)) * size / face.UnitsPerEm;

    [Fact]
    public void Shape_TwoGlyphs_CorrectIdsAndClusters()
    {
        var shaper = new SimpleShaper(LiberationSans());
        var face = SansFace();
        var font = new Font { Name = "Liberation Sans", Size = 12 };

        var glyphs = shaper.Shape("AV", font, out _);

        Assert.Equal(2, glyphs.Count);

        Assert.Equal(face.GetGlyphId('A'), glyphs[0].GlyphId);
        Assert.Equal(0, glyphs[0].Cluster);
        Assert.Equal(Advance(face, 'A', 12), glyphs[0].Advance, 10);

        Assert.Equal(face.GetGlyphId('V'), glyphs[1].GlyphId);
        Assert.Equal(1, glyphs[1].Cluster);
        Assert.Equal(Advance(face, 'V', 12), glyphs[1].Advance, 10);
    }

    [Fact]
    public void Shape_TotalAdvanceEqualsMeasureText()
    {
        var fonts = LiberationSans();
        var shaper = new SimpleShaper(fonts);
        var font = new Font { Name = "Liberation Sans", Size = 12 };

        shaper.Shape("AV", font, out var advance);

        Assert.Equal(fonts.MeasureText("AV", font), advance, 10);
    }

    [Fact]
    public void Shape_NoKerning_AdvanceIsSumOfGlyphAdvances()
    {
        var shaper = new SimpleShaper(LiberationSans());
        var font = new Font { Name = "Liberation Sans", Size = 12 };

        var glyphs = shaper.Shape("AV", font, out var advance);

        // Legacy 'kern' is not applied by default: total == sum, not sum - 152 units.
        Assert.Equal(glyphs[0].Advance + glyphs[1].Advance, advance, 10);
    }

    [Fact]
    public void Shape_UnknownFamily_ThrowsWithNameInMessage()
    {
        var shaper = new SimpleShaper(new FontCollection());
        var font = new Font { Name = "Nonexistent Font", Size = 12 };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            shaper.Shape("AV", font, out _));
        Assert.Contains("Nonexistent Font", ex.Message);
    }
}
