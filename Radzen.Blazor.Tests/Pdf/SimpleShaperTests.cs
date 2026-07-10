#nullable enable
using System;
using System.IO;
using Xunit;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Blazor.Pdf.Tests;

// Contract pinned for the v1 text pipeline seams (F5): ITextShaper / SimpleShaper /
// GlyphRun / PositionedGlyph.
//
// Pinned shapes:
//  - interface ITextShaper { GlyphRun Shape(ReadOnlySpan<char> text, Font font,
//        FlowDirection direction, WritingMode mode); }
//  - GlyphRun { IReadOnlyList<PositionedGlyph> Glyphs; Font Font; double Advance; }
//  - PositionedGlyph { ushort GlyphId; double Advance; int Cluster; }
//  - SimpleShaper(FontCollection fonts) : ITextShaper. Identity codepoint->glyph via the
//    resolved font's cmap; per-glyph Advance = hmtx advance * font.Size / UnitsPerEm;
//    GlyphRun.Advance = sum of per-glyph advances.
//  - Only FlowDirection.LeftToRight + WritingMode.HorizontalTopToBottom are supported in
//    v1; any other direction or mode throws NotSupportedException.
//
// KERN FINDING (corrects the task's assumption): Liberation Sans DOES ship a legacy
// format-0 'kern' table (908 pairs; A/V = -152 units). The task guessed GPOS-only.
// However v1 pins NO kerning: MeasureText and GlyphRun.Advance are both pure hmtx sums,
// so they stay equal (asserted below). Applying legacy 'kern' is a deliberately deferred
// future-shaper concern; without it, shaped "AV" advance == A.advance + V.advance.
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

        var run = shaper.Shape("AV", font, FlowDirection.LeftToRight, WritingMode.HorizontalTopToBottom);

        Assert.Equal(2, run.Glyphs.Count);

        Assert.Equal(face.GetGlyphId('A'), run.Glyphs[0].GlyphId);
        Assert.Equal(0, run.Glyphs[0].Cluster);
        Assert.Equal(Advance(face, 'A', 12), run.Glyphs[0].Advance, 10);

        Assert.Equal(face.GetGlyphId('V'), run.Glyphs[1].GlyphId);
        Assert.Equal(1, run.Glyphs[1].Cluster);
        Assert.Equal(Advance(face, 'V', 12), run.Glyphs[1].Advance, 10);
    }

    [Fact]
    public void Shape_RunFontMatchesRequest()
    {
        var shaper = new SimpleShaper(LiberationSans());
        var font = new Font { Name = "Liberation Sans", Size = 12 };

        var run = shaper.Shape("AV", font, FlowDirection.LeftToRight, WritingMode.HorizontalTopToBottom);

        Assert.Equal("Liberation Sans", run.Font.Name);
        Assert.Equal(12d, run.Font.Size);
    }

    [Fact]
    public void Shape_TotalAdvanceEqualsMeasureText()
    {
        var fonts = LiberationSans();
        var shaper = new SimpleShaper(fonts);
        var font = new Font { Name = "Liberation Sans", Size = 12 };

        var run = shaper.Shape("AV", font, FlowDirection.LeftToRight, WritingMode.HorizontalTopToBottom);

        Assert.Equal(fonts.MeasureText("AV", font), run.Advance, 10);
    }

    [Fact]
    public void Shape_NoKerning_AdvanceIsSumOfGlyphAdvances()
    {
        var shaper = new SimpleShaper(LiberationSans());
        var font = new Font { Name = "Liberation Sans", Size = 12 };

        var run = shaper.Shape("AV", font, FlowDirection.LeftToRight, WritingMode.HorizontalTopToBottom);

        // Legacy 'kern' is not applied in v1: total == sum, not sum - 152 units.
        Assert.Equal(run.Glyphs[0].Advance + run.Glyphs[1].Advance, run.Advance, 10);
    }

    [Fact]
    public void Shape_RightToLeft_ThrowsNotSupported()
    {
        var shaper = new SimpleShaper(LiberationSans());
        var font = new Font { Name = "Liberation Sans", Size = 12 };

        Assert.Throws<NotSupportedException>(() =>
            shaper.Shape("AV", font, FlowDirection.RightToLeft, WritingMode.HorizontalTopToBottom));
    }

    [Fact]
    public void Shape_VerticalRightToLeft_ThrowsNotSupported()
    {
        var shaper = new SimpleShaper(LiberationSans());
        var font = new Font { Name = "Liberation Sans", Size = 12 };

        Assert.Throws<NotSupportedException>(() =>
            shaper.Shape("AV", font, FlowDirection.LeftToRight, WritingMode.VerticalRightToLeft));
    }

    [Fact]
    public void Shape_VerticalLeftToRight_ThrowsNotSupported()
    {
        var shaper = new SimpleShaper(LiberationSans());
        var font = new Font { Name = "Liberation Sans", Size = 12 };

        Assert.Throws<NotSupportedException>(() =>
            shaper.Shape("AV", font, FlowDirection.LeftToRight, WritingMode.VerticalLeftToRight));
    }

    [Fact]
    public void Shape_UnknownFamily_ThrowsWithNameInMessage()
    {
        var shaper = new SimpleShaper(new FontCollection());
        var font = new Font { Name = "Nonexistent Font", Size = 12 };

        var ex = Assert.Throws<InvalidOperationException>(() =>
            shaper.Shape("AV", font, FlowDirection.LeftToRight, WritingMode.HorizontalTopToBottom));
        Assert.Contains("Nonexistent Font", ex.Message);
    }
}
