#nullable enable
using System.IO;
using Xunit;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Blazor.Pdf.Tests;

// Embedded-font pair kerning parsed from the legacy 'kern' table (format 0). Liberation
// Sans ships an OpenType version-0 'kern' with 908 horizontal format-0 pairs
// (unitsPerEm 2048): A/V = -152, T/o = -227, W/a = -76 design units. The opt-in
// SimpleShaper applies those adjustments to glyph advances; the default shaper does not,
// so byte output of the existing pipeline is unchanged.
public class EmbeddedKerningTests
{
    private static SfntFont SansFace()
        => SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf"));

    private static FontCollection Fonts()
    {
        var fonts = new FontCollection();
        fonts.Register("Liberation Sans", new MemoryStream(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        return fonts;
    }

    [Fact]
    public void GetKerning_ClassicPairsMatchKernTable()
    {
        var face = SansFace();
        Assert.Equal(-152, face.GetKerning(face.GetGlyphId('A'), face.GetGlyphId('V')));
        Assert.Equal(-227, face.GetKerning(face.GetGlyphId('T'), face.GetGlyphId('o')));
        Assert.Equal(-76, face.GetKerning(face.GetGlyphId('W'), face.GetGlyphId('a')));
    }

    [Fact]
    public void GetKerning_ClassicPairsAreNegative()
    {
        var face = SansFace();
        Assert.True(face.GetKerning(face.GetGlyphId('A'), face.GetGlyphId('V')) < 0);
    }

    [Fact]
    public void GetKerning_UnkernedPairIsZero()
    {
        var face = SansFace();
        Assert.Equal(0, face.GetKerning(face.GetGlyphId('H'), face.GetGlyphId('H')));
    }

    [Fact]
    public void DefaultShaper_DoesNotApplyKerning()
    {
        var shaper = new SimpleShaper(Fonts());
        var font = new Font { Name = "Liberation Sans", Size = 12 };

        var run = shaper.Shape("AV", font, FlowDirection.LeftToRight, WritingMode.HorizontalTopToBottom);

        Assert.Equal(run.Glyphs[0].Advance + run.Glyphs[1].Advance, run.Advance, 10);
    }

    [Fact]
    public void KerningShaper_AppliesNegativeAdjustmentToLeftGlyph()
    {
        var face = SansFace();
        var font = new Font { Name = "Liberation Sans", Size = 12 };
        var expectedKern = face.GetKerning(face.GetGlyphId('A'), face.GetGlyphId('V')) * font.Size / face.UnitsPerEm;
        Assert.True(expectedKern < 0);

        var plain = new SimpleShaper(Fonts()).Shape("AV", font, FlowDirection.LeftToRight, WritingMode.HorizontalTopToBottom);
        var kerned = new SimpleShaper(Fonts(), enableKerning: true)
            .Shape("AV", font, FlowDirection.LeftToRight, WritingMode.HorizontalTopToBottom);

        // Total advance is tightened by exactly the kern amount.
        Assert.Equal(plain.Advance + expectedKern, kerned.Advance, 9);
        // The adjustment lands on the left glyph's advance; the right glyph is unchanged.
        Assert.Equal(plain.Glyphs[0].Advance + expectedKern, kerned.Glyphs[0].Advance, 9);
        Assert.Equal(plain.Glyphs[1].Advance, kerned.Glyphs[1].Advance, 9);
    }

    [Fact]
    public void KerningShaper_LeavesUnkernedPairUnchanged()
    {
        var font = new Font { Name = "Liberation Sans", Size = 12 };
        var plain = new SimpleShaper(Fonts()).Shape("HH", font, FlowDirection.LeftToRight, WritingMode.HorizontalTopToBottom);
        var kerned = new SimpleShaper(Fonts(), enableKerning: true)
            .Shape("HH", font, FlowDirection.LeftToRight, WritingMode.HorizontalTopToBottom);

        Assert.Equal(plain.Advance, kerned.Advance, 10);
    }
}
