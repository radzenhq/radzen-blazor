#nullable enable
using System;
using System.Collections.Generic;
using Xunit;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Blazor.Pdf.Tests;

// Contract for GlyfSubsetter.Subset(SfntFont, IReadOnlyCollection<ushort>).
// Identity glyph-ID model (PDF CIDFontType2): numGlyphs is preserved, loca keeps
// the original entry count, but only glyphs in the closure of the requested set
// (requested + recursive composite components + glyph 0) keep outlines.
//
// Glyph IDs and advances derived from LiberationSans-Regular.ttf via fontTools 4.60.2:
//   'H'=43 'e'=72 'l'=79 'o'=82 'W'=58 'r'=85 'd'=71 space=3
//   advances: H=1479 e=1139 l=455 o=1139
//   eacute U+00E9 = gid 171, composite referencing e(72) and acute(118)
//   numGlyphs=2620, glyph 0 = .notdef
//
// "Has an outline" is asserted by reading loca spans from the output bytes
// (SfntChecksumValidator.ReadLocaOffsets): span > 0 means the glyph has an outline.
public class GlyfSubsetterTests
{
    private const int NumGlyphs = 2620;
    private const ushort GidNotdef = 0;
    private const ushort GidH = 43;
    private const ushort GidE = 72;
    private const ushort GidL = 79;
    private const ushort GidO = 82;
    private const ushort GidW = 58;
    private const ushort GidR = 85;
    private const ushort GidD = 71;
    private const ushort GidSpace = 3;
    private const ushort GidEacute = 171;
    private const ushort GidAcute = 118;

    private static SfntFont LiberationSansRegular()
        => SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf"));

    private static byte[] OriginalBytes()
        => PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf");

    private static ushort[] GlyphIds(SfntFont font, string text)
    {
        var ids = new ushort[text.Length];
        for (var i = 0; i < text.Length; i++)
        {
            ids[i] = font.GetGlyphId(text[i]);
        }

        return ids;
    }

    // 1. Basic subset round-trips through the parser.
    [Fact]
    public void Subset_Hello_ReparsesWithSameCoreProperties()
    {
        var font = LiberationSansRegular();
        var ids = GlyphIds(font, "Hello");

        var subset = GlyfSubsetter.Subset(font, ids);
        var reparsed = SfntFont.Parse(subset);

        Assert.Equal(NumGlyphs, (int)reparsed.GlyphCount);
        Assert.Equal("Liberation Sans", reparsed.FamilyName);
    }

    [Fact]
    public void Subset_Hello_PreservesRequestedAdvances()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello"));
        var reparsed = SfntFont.Parse(subset);

        Assert.Equal(1479, (int)reparsed.GetAdvanceWidth(GidH));
        Assert.Equal(1139, (int)reparsed.GetAdvanceWidth(GidE));
        Assert.Equal(455, (int)reparsed.GetAdvanceWidth(GidL));
        Assert.Equal(1139, (int)reparsed.GetAdvanceWidth(GidO));
    }

    [Fact]
    public void Subset_Hello_PreservesAllAdvancesNotJustRequested()
    {
        // hmtx is copied through in full: advances survive for glyphs outside the closure.
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello"));
        var reparsed = SfntFont.Parse(subset);

        Assert.Equal(1933, (int)reparsed.GetAdvanceWidth(GidW));
        Assert.Equal(682, (int)reparsed.GetAdvanceWidth(GidR));
        Assert.Equal(569, (int)reparsed.GetAdvanceWidth(GidSpace));
    }

    [Fact]
    public void Subset_Hello_PreservesCmapMappings()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello"));
        var reparsed = SfntFont.Parse(subset);

        Assert.Equal(GidH, reparsed.GetGlyphId('H'));
        Assert.Equal(GidE, reparsed.GetGlyphId('e'));
        Assert.Equal(GidL, reparsed.GetGlyphId('l'));
        Assert.Equal(GidO, reparsed.GetGlyphId('o'));
    }

    [Fact]
    public void Subset_Hello_ClosureHasOutlinesRequestedAndNotdef()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello"));
        var loca = SfntChecksumValidator.ReadLocaOffsets(subset);

        Assert.Equal(NumGlyphs + 1, loca.Length);
        foreach (var gid in new[] { GidNotdef, GidH, GidE, GidL, GidO })
        {
            Assert.True(Span(loca, gid) > 0, $"expected outline for glyph {gid}");
        }
    }

    // 2. Composite closure: subset ONLY eacute, its components must survive.
    [Fact]
    public void Subset_Eacute_PullsInComponentGlyphs()
    {
        var font = LiberationSansRegular();
        Assert.Equal(GidEacute, font.GetGlyphId(0x00E9));

        var subset = GlyfSubsetter.Subset(font, new ushort[] { GidEacute });
        var loca = SfntChecksumValidator.ReadLocaOffsets(subset);

        Assert.True(Span(loca, GidEacute) > 0, "eacute must keep its outline");
        Assert.True(Span(loca, GidE) > 0, "component 'e' must be retained");
        Assert.True(Span(loca, GidAcute) > 0, "component 'acute' must be retained");
        Assert.True(Span(loca, GidNotdef) > 0, ".notdef must be retained");
    }

    [Fact]
    public void Subset_Eacute_ReparsedComponentAdvancesPreserved()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, new ushort[] { GidEacute });
        var reparsed = SfntFont.Parse(subset);

        Assert.Equal(1139, (int)reparsed.GetAdvanceWidth(GidE));
        Assert.Equal(682, (int)reparsed.GetAdvanceWidth(GidAcute));
    }

    [Fact]
    public void Subset_Eacute_UnrelatedGlyphIsEmpty()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, new ushort[] { GidEacute });
        var loca = SfntChecksumValidator.ReadLocaOffsets(subset);

        // 'H' is neither requested nor a component of eacute.
        Assert.Equal(0, Span(loca, GidH));
    }

    // 3. Glyphs outside the closure have zero-length loca spans.
    [Fact]
    public void Subset_Hello_GlyphOutsideClosureIsEmpty()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello"));
        var loca = SfntChecksumValidator.ReadLocaOffsets(subset);

        // 'W' has an outline in the original but is not in the "Hello" closure.
        Assert.Equal(0, Span(loca, GidW));
    }

    // 4. .notdef always retained even when not requested.
    [Fact]
    public void Subset_WithoutNotdef_StillKeepsNotdefOutline()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, new ushort[] { GidO });
        var loca = SfntChecksumValidator.ReadLocaOffsets(subset);

        Assert.True(Span(loca, GidNotdef) > 0);
        Assert.True(Span(loca, GidO) > 0);
    }

    // 5. Size: closure of "Hello World" is a small fraction of the 410KB original.
    [Fact]
    public void Subset_HelloWorld_IsMuchSmallerThanOriginal()
    {
        var original = OriginalBytes();
        var font = SfntFont.Parse(original);
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello World"));

        Assert.True(subset.Length < original.Length / 10,
            $"subset {subset.Length} bytes should be < 10% of original {original.Length}");
    }

    // 6. Checksums: every directory entry and head.checkSumAdjustment are valid.
    [Fact]
    public void Subset_TableChecksumsAreCorrect()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello"));

        SfntChecksumValidator.AssertTableChecksums(subset);
    }

    [Fact]
    public void Subset_HeadCheckSumAdjustmentIsCorrect()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello"));

        SfntChecksumValidator.AssertHeadAdjustment(subset);
    }

    // 7. loca offsets are non-decreasing and the last equals glyf length.
    [Fact]
    public void Subset_LocaIsMonotonicAndMatchesGlyfLength()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello"));
        var loca = SfntChecksumValidator.ReadLocaOffsets(subset);

        for (var i = 1; i < loca.Length; i++)
        {
            Assert.True(loca[i] >= loca[i - 1], $"loca not monotonic at {i}");
        }

        Assert.True(SfntFont.Parse(subset).TryGetTable("glyf", out var glyf));
        Assert.Equal((uint)glyf.Length, loca[^1]);
    }

    [Fact]
    public void Subset_HeadIndexToLocFormatMatchesLocaEncoding()
    {
        // The subsetter chooses loca format via head.indexToLocFormat; whatever it
        // picks (0=short, 1=long) the head field must agree so re-parse succeeds.
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello"));

        var format = SfntChecksumValidator.IndexToLocFormat(subset);
        Assert.True(format == 0 || format == 1);

        // ReadLocaOffsets honours the same field; last offset must equal glyf length.
        var loca = SfntChecksumValidator.ReadLocaOffsets(subset);
        Assert.True(SfntFont.Parse(subset).TryGetTable("glyf", out var glyf));
        Assert.Equal((uint)glyf.Length, loca[^1]);
    }

    // 8. Layout tables stripped; required tables retained.
    [Fact]
    public void Subset_StripsLayoutTables()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello"));
        var reparsed = SfntFont.Parse(subset);

        Assert.False(reparsed.TryGetTable("GSUB", out _));
        Assert.False(reparsed.TryGetTable("GPOS", out _));
        Assert.False(reparsed.TryGetTable("GDEF", out _));
        Assert.False(reparsed.TryGetTable("BASE", out _));
    }

    [Theory]
    [InlineData("cmap")]
    [InlineData("name")]
    [InlineData("OS/2")]
    [InlineData("post")]
    [InlineData("hhea")]
    [InlineData("hmtx")]
    [InlineData("maxp")]
    [InlineData("head")]
    [InlineData("glyf")]
    [InlineData("loca")]
    public void Subset_RetainsRequiredTables(string tag)
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello"));

        Assert.True(SfntFont.Parse(subset).TryGetTable(tag, out var data));
        Assert.NotNull(data);
        Assert.True(data.Length > 0);
    }

    // 9. Error cases.
    [Fact]
    public void Subset_CffFont_Throws()
    {
        // NotoSansSC-Subset.otf is CFF-flavoured; glyf subsetting is TrueType-only (F4b covers CFF).
        var font = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf"));

        Assert.ThrowsAny<Exception>(() => GlyfSubsetter.Subset(font, new ushort[] { 34 }));
    }

    [Fact]
    public void Subset_EmptyGlyphSet_YieldsValidFontWithNotdef()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, Array.Empty<ushort>());

        var reparsed = SfntFont.Parse(subset);
        Assert.Equal(NumGlyphs, (int)reparsed.GlyphCount);
        Assert.Equal("Liberation Sans", reparsed.FamilyName);

        var loca = SfntChecksumValidator.ReadLocaOffsets(subset);
        Assert.True(Span(loca, GidNotdef) > 0);
        SfntChecksumValidator.AssertTableChecksums(subset);
        SfntChecksumValidator.AssertHeadAdjustment(subset);
    }

    // 10. Determinism.
    [Fact]
    public void Subset_IsDeterministic()
    {
        var font = LiberationSansRegular();
        var ids = GlyphIds(font, "Hello World");

        var first = GlyfSubsetter.Subset(font, ids);
        var second = GlyfSubsetter.Subset(font, ids);

        Assert.Equal(first, second);
    }

    private static long Span(uint[] loca, ushort gid) => (long)loca[gid + 1] - loca[gid];
}
