#nullable enable
using System;
using System.Linq;
using Xunit;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Radzen.Documents.Pdf.Fonts.Cff;

namespace Radzen.Blazor.Pdf.Tests;

// Hardening for the two font subsetters: an out-of-range glyph id (as a corrupt
// font's cmap could yield) must fail loudly and identically in the CFF and glyf
// paths, and the glyf subset must carry the hinting tables its outline bytecode
// depends on. Fixtures reuse the shipped LiberationSans (TrueType, has
// cvt/fpgm/prep) and NotoSansSC-Subset (CID-keyed CFF).
public class FontSubsetHardeningTests
{
    private static SfntFont LiberationSansRegular()
        => SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf"));

    private static CffFont NotoCff()
    {
        var sfnt = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf"));
        Assert.True(sfnt.TryGetTable("CFF ", out var cff));
        return CffFont.Parse(cff);
    }

    [Fact]
    public void Glyf_OutOfRangeGid_ThrowsWithGidAndCount()
    {
        var font = LiberationSansRegular();
        var badGid = (ushort)(font.GlyphCount + 5);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => GlyfSubsetter.Subset(font, new[] { badGid }));

        Assert.Contains(badGid.ToString(), ex.Message, StringComparison.Ordinal);
        Assert.Contains(font.GlyphCount.ToString(), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Glyf_BuildCompactGidMap_OutOfRangeGid_Throws()
    {
        var font = LiberationSansRegular();
        var badGid = (ushort)(font.GlyphCount + 5);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => GlyfSubsetter.BuildCompactGidMap(font, new[] { badGid }));
    }

    [Fact]
    public void Cff_OutOfRangeGid_ThrowsWithGidAndCount()
    {
        var font = NotoCff();
        var badGid = font.GlyphCount + 5;

        var ex = Assert.Throws<ArgumentOutOfRangeException>(
            () => CffSubsetter.Subset(font, new[] { badGid }));

        Assert.Contains(badGid.ToString(), ex.Message, StringComparison.Ordinal);
        Assert.Contains(font.GlyphCount.ToString(), ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Cff_NegativeGid_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CffSubsetter.Subset(NotoCff(), new[] { -1 }));
    }

    [Fact]
    public void BothPaths_ThrowSameExceptionType_ForOutOfRangeGid()
    {
        var glyf = Record.Exception(() =>
            GlyfSubsetter.Subset(LiberationSansRegular(), new ushort[] { 60000 }));
        var cff = Record.Exception(() =>
            CffSubsetter.Subset(NotoCff(), new[] { 60000 }));

        Assert.IsType<ArgumentOutOfRangeException>(glyf);
        Assert.IsType<ArgumentOutOfRangeException>(cff);
    }

    [Fact]
    public void Glyf_InRangeSubset_IsUnaffected()
    {
        var font = LiberationSansRegular();
        var ids = new[] { font.GetGlyphId('H'), font.GetGlyphId('i') };

        var subset = GlyfSubsetter.Subset(font, ids);
        var reparsed = SfntFont.Parse(subset);

        Assert.True(reparsed.GlyphCount >= 3); // notdef + H + i
    }

    [Fact]
    public void Cff_InRangeSubset_IsUnaffected()
    {
        var subset = CffFont.Parse(CffSubsetter.Subset(NotoCff(), new[] { 34, 66 }));

        Assert.Equal(3, subset.GlyphCount); // {0, 34, 66}
    }

    [Fact]
    public void Glyf_Subset_CopiesHintingTablesThrough()
    {
        var font = LiberationSansRegular();
        var subset = SfntFont.Parse(GlyfSubsetter.Subset(font, GlyphIds(font, "Hello")));

        foreach (var tag in new[] { "cvt ", "fpgm", "prep" })
        {
            Assert.True(font.TryGetTable(tag, out var original), $"fixture must have {tag}");
            Assert.True(subset.TryGetTable(tag, out var copied), $"subset must keep {tag}");
            Assert.Equal(original, copied);
        }
    }

    [Fact]
    public void Glyf_Subset_KeepsGlyphInstructionsIntact()
    {
        // 'H' carries instruction bytecode; the compact subset copies the outline
        // (including instructions) verbatim, so it survives byte-identical.
        var font = LiberationSansRegular();
        var gidH = font.GetGlyphId('H');
        var expected = Type0EmbedSupport.OutlineBytes(font, gidH);

        var subset = GlyfSubsetter.Subset(font, new[] { gidH });
        var reparsed = SfntFont.Parse(subset);
        var loca = SfntChecksumValidator.ReadLocaOffsets(subset);
        Assert.True(reparsed.TryGetTable("glyf", out var glyf));

        var found = false;
        for (var gid = 0; gid < reparsed.GlyphCount; gid++)
        {
            if (glyf[(int)loca[gid]..(int)loca[gid + 1]].AsSpan().SequenceEqual(expected))
            {
                found = true;
            }
        }

        Assert.True(found, "'H' outline (with instructions) must survive byte-identical");
    }

    [Fact]
    public void Glyf_Subset_FontWithoutHintingTables_SubsetsFine()
    {
        var stripped = SfntFont.Parse(StripTables(
            PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf"), "cvt ", "fpgm", "prep"));

        Assert.False(stripped.TryGetTable("cvt ", out _));

        var subset = SfntFont.Parse(GlyfSubsetter.Subset(stripped, GlyphIds(stripped, "Hi")));

        Assert.False(subset.TryGetTable("cvt ", out _));
        Assert.False(subset.TryGetTable("fpgm", out _));
        Assert.False(subset.TryGetTable("prep", out _));
        Assert.True(subset.GlyphCount >= 3);
    }

    private static ushort[] GlyphIds(SfntFont font, string text)
        => text.Select(ch => font.GetGlyphId(ch)).ToArray();

    // Removes directory records for the given tags by compacting the table
    // directory and decrementing numTables; table data stays in place so all
    // surviving offsets remain valid.
    private static byte[] StripTables(byte[] font, params string[] tags)
    {
        var result = (byte[])font.Clone();
        var numTables = (result[4] << 8) | result[5];
        var strip = tags.ToHashSet();

        var write = 12;
        var kept = 0;
        for (var i = 0; i < numTables; i++)
        {
            var rec = 12 + i * 16;
            var tag = System.Text.Encoding.ASCII.GetString(result, rec, 4);
            if (strip.Contains(tag))
            {
                continue;
            }

            Array.Copy(result, rec, result, write, 16);
            write += 16;
            kept++;
        }

        result[4] = (byte)(kept >> 8);
        result[5] = (byte)kept;
        return result;
    }
}
