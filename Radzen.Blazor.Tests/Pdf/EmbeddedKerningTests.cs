#nullable enable
using System.IO;
using Xunit;
using Radzen.Documents.Pdf;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

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
        var font = new Font { Family = "Liberation Sans", Size = 12 };

        var glyphs = shaper.Shape("AV", font, out var advance);

        Assert.Equal(glyphs[0].Advance + glyphs[1].Advance, advance, 10);
    }

    [Fact]
    public void KerningShaper_AppliesNegativeAdjustmentToLeftGlyph()
    {
        var face = SansFace();
        var font = new Font { Family = "Liberation Sans", Size = 12 };
        var expectedKern = face.GetKerning(face.GetGlyphId('A'), face.GetGlyphId('V')) * font.Size!.Value.Point / face.UnitsPerEm;
        Assert.True(expectedKern < 0);

        var plain = new SimpleShaper(Fonts()).Shape("AV", font, out var plainAdvance);
        var kerned = new SimpleShaper(Fonts(), enableKerning: true).Shape("AV", font, out var kernedAdvance);

        Assert.Equal(plainAdvance + expectedKern, kernedAdvance, 9);
        Assert.Equal(plain[0].Advance + expectedKern, kerned[0].Advance, 9);
        Assert.Equal(plain[1].Advance, kerned[1].Advance, 9);
    }

    [Fact]
    public void KerningShaper_LeavesUnkernedPairUnchanged()
    {
        var font = new Font { Family = "Liberation Sans", Size = 12 };
        new SimpleShaper(Fonts()).Shape("HH", font, out var plainAdvance);
        new SimpleShaper(Fonts(), enableKerning: true).Shape("HH", font, out var kernedAdvance);

        Assert.Equal(plainAdvance, kernedAdvance, 10);
    }

    [Fact]
    public void KerningShaper_DoesNotKernAcrossSpaces()
    {
        var font = new Font { Family = "Liberation Sans", Size = 12 };
        new SimpleShaper(Fonts()).Shape("A A", font, out var plainAdvance);
        new SimpleShaper(Fonts(), enableKerning: true).Shape("A A", font, out var kernedAdvance);

        Assert.Equal(plainAdvance, kernedAdvance, 9);
    }

    [Fact]
    public void KerningShaper_StillKernsNonSpacePairsWhenSpacePresent()
    {
        var face = SansFace();
        var font = new Font { Family = "Liberation Sans", Size = 12 };
        var avKern = face.GetKerning(face.GetGlyphId('A'), face.GetGlyphId('V')) * font.Size!.Value.Point / face.UnitsPerEm;

        new SimpleShaper(Fonts()).Shape("AV A", font, out var plainAdvance);
        new SimpleShaper(Fonts(), enableKerning: true).Shape("AV A", font, out var kernedAdvance);

        Assert.Equal(plainAdvance + avKern, kernedAdvance, 9);
    }
}
