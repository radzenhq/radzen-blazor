#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Radzen.Documents.Pdf.Fonts.Sfnt;

namespace Radzen.Blazor.Pdf.Tests;

// Contract for GlyfSubsetter.Subset(SfntFont, IReadOnlyCollection<ushort>).
// COMPACT glyph-ID model (PDF CIDFontType2 with content codes == new gids):
// the subset renumbers glyphs into a contiguous space 0..N-1 holding exactly the
// closure of the requested set (requested + recursive composite components +
// glyph 0), with compact loca/glyf/hmtx sized for N glyphs. Composite glyphs have
// their component ids rewritten into the compact space; simple-glyph outlines
// survive with their contours/points intact but with instruction bytecode STRIPPED
// (the subset drops the cvt/fpgm/prep hinting tables to stay compact). The compact
// gid assignment is recovered structurally (outline bytes, advance multisets)
// rather than pinned to an order.
//
// Glyph IDs and advances derived from LiberationSans-Regular.ttf via fontTools 4.60.2:
//   'H'=43 'e'=72 'l'=79 'o'=82 'W'=58 'r'=85 'd'=71 space=3
//   advances: notdef=1229 H=1479 e=1139 l=455 o=1139 W=1933 r=682 d=1139 space=569
//   eacute U+00E9 = gid 171, composite referencing e(72) and acute(118), acute adv=682
//   original numGlyphs=2620, glyph 0 = .notdef
public class GlyfSubsetterTests
{
    private const ushort GidNotdef = 0;
    private const ushort GidH = 43;
    private const ushort GidE = 72;
    private const ushort GidO = 82;
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

    private static int ClosureCount(SfntFont font, IEnumerable<ushort> ids)
        => Type0EmbedSupport.GlyfClosure(font, ids.Select(gid => (int)gid)).Count;

    private static byte[][] SubsetOutlines(byte[] subset)
    {
        var reparsed = SfntFont.Parse(subset);
        var loca = SfntChecksumValidator.ReadLocaOffsets(subset);
        Assert.True(reparsed.TryGetTable("glyf", out var glyf));

        var outlines = new byte[reparsed.GlyphCount][];
        for (var gid = 0; gid < reparsed.GlyphCount; gid++)
        {
            outlines[gid] = glyf[(int)loca[gid]..(int)loca[gid + 1]];
        }

        return outlines;
    }

    // The subset strips instruction bytecode from every glyph, so a source outline
    // survives with its contours/points intact but zero instructions. Normalize the
    // expected simple-glyph bytes the same way the subsetter does before matching.
    private static int FindOutline(byte[][] outlines, byte[] expected)
    {
        var target = StripSimpleInstructions(expected);
        for (var gid = 0; gid < outlines.Length; gid++)
        {
            if (outlines[gid].AsSpan().SequenceEqual(target))
            {
                return gid;
            }
        }

        return -1;
    }

    // Mirrors GlyfSubsetter: set instructionLength to 0, drop the instructions[]
    // block, keep flags/coordinates, and pad an odd result to a 2-byte boundary.
    private static byte[] StripSimpleInstructions(byte[] g)
    {
        if (g.Length < 10 || (short)((g[0] << 8) | g[1]) < 0)
        {
            return g; // empty or composite outline
        }

        var contours = (short)((g[0] << 8) | g[1]);
        var instrLenPos = 10 + contours * 2;
        var instrLen = (g[instrLenPos] << 8) | g[instrLenPos + 1];
        var headLen = instrLenPos + 2;
        var tailStart = headLen + instrLen;
        var tailLen = g.Length - tailStart;

        var written = headLen + tailLen;
        var result = new byte[(written & 1) != 0 ? written + 1 : written];
        Array.Copy(g, 0, result, 0, headLen);
        result[headLen - 2] = 0;
        result[headLen - 1] = 0;
        Array.Copy(g, tailStart, result, headLen, tailLen);
        return result;
    }

    // 1. Basic subset round-trips through the parser with a COMPACT glyph count.
    [Fact]
    public void Subset_Hello_ReparsesWithCompactGlyphCount()
    {
        var font = LiberationSansRegular();
        var ids = GlyphIds(font, "Hello");

        var subset = GlyfSubsetter.Subset(font, ids);
        var reparsed = SfntFont.Parse(subset);

        // closure("Hello") = {notdef, H, e, l, o}
        Assert.Equal(5, (int)reparsed.GlyphCount);
        Assert.Equal(ClosureCount(font, ids), (int)reparsed.GlyphCount);
    }

    [Fact]
    public void Subset_Hello_PreservesAdvancesInCompactSpace()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello"));
        var reparsed = SfntFont.Parse(subset);

        var actual = new List<int>();
        for (ushort gid = 0; gid < reparsed.GlyphCount; gid++)
        {
            actual.Add(reparsed.GetAdvanceWidth(gid));
        }

        var expected = new List<int>
        {
            font.GetAdvanceWidth(GidNotdef), 1479, 1139, 455, 1139,
        };

        Assert.Equal(expected.Order(), actual.Order());
    }

    [Fact]
    public void Subset_Hello_HmtxIsCompact()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello"));
        var reparsed = SfntFont.Parse(subset);

        Assert.True(reparsed.TryGetTable("hmtx", out var hmtx));
        Assert.True(hmtx.Length <= 4 * (int)reparsed.GlyphCount,
            $"hmtx {hmtx.Length} bytes exceeds compact bound {4 * reparsed.GlyphCount}");
    }

    [Fact]
    public void Subset_Hello_SimpleOutlineShapesSurviveWithInstructionsStripped()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello"));
        var outlines = SubsetOutlines(subset);

        foreach (var gid in new[] { GidNotdef, GidH, GidE, GidO })
        {
            var original = Type0EmbedSupport.OutlineBytes(font, gid);
            Assert.True(original.Length > 0);
            Assert.True(FindOutline(outlines, original) >= 0, $"outline of original gid {gid} missing from subset");
        }
    }

    // 2. Composite closure: subset ONLY eacute; components come along, renumbered.
    [Fact]
    public void Subset_Eacute_PullsComponentsIntoCompactSpace()
    {
        var font = LiberationSansRegular();
        Assert.Equal(GidEacute, font.GetGlyphId(0x00E9));

        var subset = GlyfSubsetter.Subset(font, new ushort[] { GidEacute });
        var outlines = SubsetOutlines(subset);

        // closure(eacute) = {notdef, eacute, e, acute}
        Assert.Equal(4, outlines.Length);

        // The simple components keep their outline bytes at some compact gid.
        var eGid = FindOutline(outlines, Type0EmbedSupport.OutlineBytes(font, GidE));
        var acuteGid = FindOutline(outlines, Type0EmbedSupport.OutlineBytes(font, GidAcute));
        Assert.True(eGid >= 0, "component 'e' outline missing");
        Assert.True(acuteGid >= 0, "component 'acute' outline missing");
        Assert.NotEqual(eGid, acuteGid);
    }

    [Fact]
    public void Subset_Eacute_RewritesCompositeComponentIds()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, new ushort[] { GidEacute });
        var outlines = SubsetOutlines(subset);

        var eGid = FindOutline(outlines, Type0EmbedSupport.OutlineBytes(font, GidE));
        var acuteGid = FindOutline(outlines, Type0EmbedSupport.OutlineBytes(font, GidAcute));

        // Exactly one composite glyph; its component ids point at the compact
        // gids carrying the e/acute outlines.
        var composites = new List<int>();
        for (var gid = 0; gid < outlines.Length; gid++)
        {
            if (outlines[gid].Length >= 10 && (short)((outlines[gid][0] << 8) | outlines[gid][1]) < 0)
            {
                composites.Add(gid);
            }
        }

        var composite = Assert.Single(composites);
        var components = CompositeComponents(outlines[composite]);
        Assert.Equal(
            new[] { eGid, acuteGid }.Order(),
            components.Order());
    }

    [Fact]
    public void Subset_Eacute_ComponentAdvancesPreserved()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, new ushort[] { GidEacute });
        var reparsed = SfntFont.Parse(subset);
        var outlines = SubsetOutlines(subset);

        var eGid = FindOutline(outlines, Type0EmbedSupport.OutlineBytes(font, GidE));
        var acuteGid = FindOutline(outlines, Type0EmbedSupport.OutlineBytes(font, GidAcute));

        Assert.Equal(1139, (int)reparsed.GetAdvanceWidth((ushort)eGid));
        Assert.Equal(682, (int)reparsed.GetAdvanceWidth((ushort)acuteGid));
    }

    // 3. Glyphs outside the closure are dropped entirely, not blanked.
    [Fact]
    public void Subset_Hello_GlyphOutsideClosureIsNotEmbedded()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello"));
        var outlines = SubsetOutlines(subset);

        // 'W' has an outline in the original but is not in the "Hello" closure.
        Assert.Equal(-1, FindOutline(outlines, Type0EmbedSupport.OutlineBytes(font, 58)));
    }

    // 4. Empty-outline glyphs in the request (space) still get a compact gid.
    [Fact]
    public void Subset_HelloWorldWithSpace_KeepsEmptyOutlineGlyph()
    {
        var font = LiberationSansRegular();
        var ids = GlyphIds(font, "Hello World");

        var subset = GlyfSubsetter.Subset(font, ids);
        var reparsed = SfntFont.Parse(subset);

        // distinct {H,e,l,o,space,W,r,d} + notdef = 9, space with an empty outline
        Assert.Equal(9, (int)reparsed.GlyphCount);
        var outlines = SubsetOutlines(subset);
        Assert.Contains(outlines, outline => outline.Length == 0);
    }

    // 5. .notdef always retained even when not requested.
    [Fact]
    public void Subset_WithoutNotdef_StillKeepsNotdefOutline()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, new ushort[] { GidO });
        var reparsed = SfntFont.Parse(subset);

        Assert.Equal(2, (int)reparsed.GlyphCount);
        var outlines = SubsetOutlines(subset);
        Assert.True(FindOutline(outlines, Type0EmbedSupport.OutlineBytes(font, GidNotdef)) >= 0);
        Assert.True(FindOutline(outlines, Type0EmbedSupport.OutlineBytes(font, GidO)) >= 0);
    }

    // 6. Size: a compact subset dwarfs both the original and the per-document
    // fixed cost of full-length loca/hmtx.
    [Fact]
    public void Subset_HelloWorld_IsMuchSmallerThanOriginal()
    {
        var original = OriginalBytes();
        var font = SfntFont.Parse(original);
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello World"));

        Assert.True(subset.Length < 16_000,
            $"compact subset is {subset.Length} bytes; full-length loca/hmtx must not pass through (original {original.Length})");
    }

    // 7. Checksums: every directory entry and head.checkSumAdjustment are valid.
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

    // 8. loca is compact (N + 1 entries), monotonic, and ends at glyf length.
    [Fact]
    public void Subset_LocaIsCompactMonotonicAndMatchesGlyfLength()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello"));
        var reparsed = SfntFont.Parse(subset);
        var loca = SfntChecksumValidator.ReadLocaOffsets(subset);

        Assert.Equal((int)reparsed.GlyphCount + 1, loca.Length);
        for (var i = 1; i < loca.Length; i++)
        {
            Assert.True(loca[i] >= loca[i - 1], $"loca not monotonic at {i}");
        }

        Assert.True(reparsed.TryGetTable("glyf", out var glyf));
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

        var loca = SfntChecksumValidator.ReadLocaOffsets(subset);
        Assert.True(SfntFont.Parse(subset).TryGetTable("glyf", out var glyf));
        Assert.Equal((uint)glyf.Length, loca[^1]);
    }

    // 9. Layout tables stripped; core outline/metric tables retained. cmap and
    // name are NOT pinned: a CIDFontType2 subset needs neither and may drop or
    // minimize them.
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
    [InlineData("head")]
    [InlineData("hhea")]
    [InlineData("hmtx")]
    [InlineData("maxp")]
    [InlineData("glyf")]
    [InlineData("loca")]
    public void Subset_RetainsCoreTables(string tag)
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello"));

        Assert.True(SfntFont.Parse(subset).TryGetTable(tag, out var data));
        Assert.NotNull(data);
        Assert.True(data.Length > 0);
    }

    // 10. Error cases.
    [Fact]
    public void Subset_CffFont_Throws()
    {
        // NotoSansSC-Subset.otf is CFF-flavoured; glyf subsetting is TrueType-only.
        var font = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf"));

        Assert.ThrowsAny<Exception>(() => GlyfSubsetter.Subset(font, new ushort[] { 34 }));
    }

    [Fact]
    public void Subset_EmptyGlyphSet_YieldsValidFontWithNotdefOnly()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, Array.Empty<ushort>());

        var reparsed = SfntFont.Parse(subset);
        Assert.Equal(1, (int)reparsed.GlyphCount);

        var outlines = SubsetOutlines(subset);
        Assert.True(FindOutline(outlines, Type0EmbedSupport.OutlineBytes(font, GidNotdef)) >= 0);
        SfntChecksumValidator.AssertTableChecksums(subset);
        SfntChecksumValidator.AssertHeadAdjustment(subset);
    }

    // 11. Determinism.
    [Fact]
    public void Subset_IsDeterministic()
    {
        var font = LiberationSansRegular();
        var ids = GlyphIds(font, "Hello World");

        var first = GlyfSubsetter.Subset(font, ids);
        var second = GlyfSubsetter.Subset(font, ids);

        Assert.Equal(first, second);
    }

    private static List<int> CompositeComponents(byte[] outline)
    {
        var components = new List<int>();
        var pos = 10;
        while (true)
        {
            var flags = (outline[pos] << 8) | outline[pos + 1];
            components.Add((outline[pos + 2] << 8) | outline[pos + 3]);
            pos += 4;
            pos += (flags & 0x0001) != 0 ? 4 : 2;
            if ((flags & 0x0008) != 0)
            {
                pos += 2;
            }
            else if ((flags & 0x0040) != 0)
            {
                pos += 4;
            }
            else if ((flags & 0x0080) != 0)
            {
                pos += 8;
            }

            if ((flags & 0x0020) == 0)
            {
                break;
            }
        }

        return components;
    }
}
