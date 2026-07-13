#nullable enable
using System;
using System.IO;
using System.Linq;
using Xunit;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Radzen.Documents.Pdf.Fonts.Cff;

namespace Radzen.Blazor.Pdf.Tests;

// Hardening for the two font subsetters: an out-of-range glyph id (as a corrupt
// font's cmap could yield) must fail loudly and identically in the CFF and glyf
// paths, and the glyf subset must STRIP glyph instructions and DROP the hinting
// tables (cvt/fpgm/prep) so the compact output stays small while outlines stay
// intact. Fixtures reuse the shipped LiberationSans (TrueType, has cvt/fpgm/prep)
// and NotoSansSC-Subset (CID-keyed CFF).
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
    public void Glyf_Subset_DropsHintingTables()
    {
        // Strategy: the compact subset strips glyph instructions, so the cvt/fpgm/prep
        // hinting tables that bytecode depended on are no longer needed and are dropped.
        var font = LiberationSansRegular();
        var subset = SfntFont.Parse(GlyfSubsetter.Subset(font, GlyphIds(font, "Hello")));

        foreach (var tag in new[] { "cvt ", "fpgm", "prep" })
        {
            Assert.True(font.TryGetTable(tag, out _), $"fixture must have {tag}");
            Assert.False(subset.TryGetTable(tag, out _), $"subset must NOT keep {tag}");
        }
    }

    [Fact]
    public void Glyf_Subset_StripsGlyphInstructions()
    {
        // 'H' carries instruction bytecode; the compact subset keeps its outline
        // (contours, points, advance width) but carries ZERO instruction bytes.
        var font = LiberationSansRegular();
        var gidH = font.GetGlyphId('H');
        var expected = ParseSimpleGlyph(Type0EmbedSupport.OutlineBytes(font, gidH));
        Assert.True(expected.InstructionLength > 0, "'H' fixture must be hinted");

        // Closure of {gidH} is {0, gidH}; ascending order assigns 'H' the new gid 1.
        var subset = GlyfSubsetter.Subset(font, new[] { gidH });
        var actual = ParseSimpleGlyph(SubsetGlyph(subset, 1));

        Assert.Equal(0, actual.InstructionLength);
        Assert.Equal(expected.EndPts, actual.EndPts);
        Assert.Equal(expected.Xs, actual.Xs);
        Assert.Equal(expected.Ys, actual.Ys);
        Assert.Equal(font.GetAdvanceWidth(gidH), SubsetAdvanceWidth(subset, 1));
    }

    [Fact]
    public void Glyf_Subset_MaxpMaxSizeOfInstructionsIsZero()
    {
        var font = LiberationSansRegular();
        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello"));
        var maxp = SfntChecksumValidator.ReadDirectory(subset)["maxp"];

        Assert.Equal(0, ReadUInt16(subset, (int)maxp.Offset + 24));
    }

    [Fact]
    public void Glyf_Subset_PreservesGlyphOutlineCoordinates()
    {
        // Stripping instructions is a rasterization-hint change only; it must move no
        // point. Every subset glyph's outline must equal its source glyph's outline.
        var font = LiberationSansRegular();
        var text = "Hg@Wq";
        var ids = GlyphIds(font, text);

        var subset = GlyfSubsetter.Subset(font, ids);
        foreach (var ch in text)
        {
            var gid = font.GetGlyphId(ch);
            var source = Type0EmbedSupport.OutlineBytes(font, gid);
            if (source.Length < 10 || (short)((source[0] << 8) | source[1]) < 0)
            {
                continue; // empty or composite; simple-glyph coordinate parse only
            }

            var expected = ParseSimpleGlyph(source);
            var newGid = FindSubsetGid(subset, expected);
            Assert.True(newGid >= 0, $"outline for '{ch}' must survive in the subset");
        }
    }

    [Fact]
    public void Glyf_Subset_OfHintedFont_IsSmallerThanOriginalHintingFootprint()
    {
        var font = LiberationSansRegular();
        var original = OriginalFootprint(font, "cvt ", "fpgm", "prep");

        var subset = GlyfSubsetter.Subset(font, GlyphIds(font, "Hello"));

        Assert.True(subset.Length < original,
            $"hinted-font subset ({subset.Length}) must be smaller than the original glyf+hinting footprint ({original})");
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

    [Fact]
    public void Parse_FontMissingHmtx_Throws()
    {
        // hmtx is required whenever hhea is present; without it every glyph silently
        // measures zero-width. Parsing must fail loud like a missing head/maxp/hhea.
        var stripped = StripTables(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf"), "hmtx");

        var ex = Assert.Throws<InvalidDataException>(() => SfntFont.Parse(stripped));
        Assert.Contains("hmtx", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Glyf_CompositeComponentOutOfRange_ThrowsInvalidData()
    {
        // Corrupt eacute (a composite referencing 'e' + acute) so its first component id
        // points past numGlyphs. The subsetter must fail loud rather than skip the component
        // and later throw an opaque KeyNotFoundException while remapping the composite.
        var bytes = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf");
        var dir = SfntChecksumValidator.ReadDirectory(bytes);
        var loca = SfntChecksumValidator.ReadLocaOffsets(bytes);
        const int eacute = 171;
        var start = (int)dir["glyf"].Offset + (int)loca[eacute];
        Assert.True((short)((bytes[start] << 8) | bytes[start + 1]) < 0, "eacute fixture must be composite");

        // First component record: flags at +10, componentGlyphId at +12.
        bytes[start + 12] = 0xEA; // 60000, well past numGlyphs (2620)
        bytes[start + 13] = 0x60;

        var font = SfntFont.Parse(bytes);
        var ex = Assert.Throws<InvalidDataException>(() => GlyfSubsetter.Subset(font, new ushort[] { eacute }));
        Assert.Contains("60000", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Cff_SeacEndcharGlyph_ThrowsNotSupported()
    {
        // Glyph 1 is a seac composition: operands adx=0 ady=0 bchar=65 achar=65 then endchar.
        // The compact identity-charset rewrite would break its base/accent reference, so the
        // subsetter rejects it rather than emit a wrong glyph.
        byte[] privateBody = [0x8B, 20]; // defaultWidthX 0
        byte[][] charStrings = [[0x0E], [0x8B, 0x8B, 0xCC, 0xCC, 0x0E]];
        var font = CffFont.Parse(CffFixtureBuilder.Build(privateBody, charStrings));

        Assert.Throws<NotSupportedException>(() => CffSubsetter.Subset(font, new[] { 1 }));
    }

    [Fact]
    public void Cff_PlainEndcharGlyph_SubsetsFine()
    {
        // Control: a normal endchar (0 operands) must NOT be misdetected as seac.
        byte[] privateBody = [0x8B, 20];
        byte[][] charStrings = [[0x0E], [0x0E]];
        var font = CffFont.Parse(CffFixtureBuilder.Build(privateBody, charStrings));

        var subset = CffFont.Parse(CffSubsetter.Subset(font, new[] { 1 }));
        Assert.Equal(2, subset.GlyphCount);
    }

    private static ushort[] GlyphIds(SfntFont font, string text)
        => text.Select(ch => font.GetGlyphId(ch)).ToArray();

    private readonly record struct SimpleGlyph(int InstructionLength, ushort[] EndPts, int[] Xs, int[] Ys);

    // Decodes a simple-glyph outline: contour endpoints, instruction length, and
    // absolute point coordinates (flags/deltas resolved). Instructions are skipped.
    private static SimpleGlyph ParseSimpleGlyph(byte[] g)
    {
        var contours = (short)((g[0] << 8) | g[1]);
        var pos = 10;
        var endPts = new ushort[contours];
        for (var i = 0; i < contours; i++, pos += 2)
        {
            endPts[i] = (ushort)((g[pos] << 8) | g[pos + 1]);
        }

        var points = contours == 0 ? 0 : endPts[contours - 1] + 1;
        var instrLen = (g[pos] << 8) | g[pos + 1];
        pos += 2 + instrLen;

        var flags = new byte[points];
        for (var i = 0; i < points;)
        {
            var f = g[pos++];
            flags[i++] = f;
            if ((f & 0x08) != 0)
            {
                var repeat = g[pos++];
                for (var r = 0; r < repeat && i < points; r++)
                {
                    flags[i++] = f;
                }
            }
        }

        var xs = ReadCoords(g, ref pos, flags, 0x02, 0x10);
        var ys = ReadCoords(g, ref pos, flags, 0x04, 0x20);
        return new SimpleGlyph(instrLen, endPts, xs, ys);
    }

    private static int[] ReadCoords(byte[] g, ref int pos, byte[] flags, byte shortBit, byte sameOrSignBit)
    {
        var coords = new int[flags.Length];
        var value = 0;
        for (var i = 0; i < flags.Length; i++)
        {
            var f = flags[i];
            if ((f & shortBit) != 0)
            {
                var d = g[pos++];
                value += (f & sameOrSignBit) != 0 ? d : -d;
            }
            else if ((f & sameOrSignBit) == 0)
            {
                value += (short)((g[pos] << 8) | g[pos + 1]);
                pos += 2;
            }

            coords[i] = value;
        }

        return coords;
    }

    private static byte[] SubsetGlyph(byte[] subset, int newGid)
    {
        var glyf = SfntChecksumValidator.ReadDirectory(subset)["glyf"];
        var loca = SfntChecksumValidator.ReadLocaOffsets(subset);
        return subset[((int)glyf.Offset + (int)loca[newGid])..((int)glyf.Offset + (int)loca[newGid + 1])];
    }

    private static ushort SubsetAdvanceWidth(byte[] subset, int newGid)
    {
        var hmtx = SfntChecksumValidator.ReadDirectory(subset)["hmtx"];
        return ReadUInt16(subset, (int)hmtx.Offset + newGid * 4);
    }

    // The compact subset's new gid whose simple-glyph outline equals the given one, or -1.
    private static int FindSubsetGid(byte[] subset, SimpleGlyph expected)
    {
        var glyf = SfntChecksumValidator.ReadDirectory(subset)["glyf"];
        var loca = SfntChecksumValidator.ReadLocaOffsets(subset);
        for (var gid = 0; gid + 1 < loca.Length; gid++)
        {
            var length = (int)(loca[gid + 1] - loca[gid]);
            if (length < 10)
            {
                continue;
            }

            var bytes = subset[((int)glyf.Offset + (int)loca[gid])..((int)glyf.Offset + (int)loca[gid + 1])];
            if ((short)((bytes[0] << 8) | bytes[1]) < 0)
            {
                continue;
            }

            var candidate = ParseSimpleGlyph(bytes);
            if (candidate.EndPts.AsSpan().SequenceEqual(expected.EndPts)
                && candidate.Xs.AsSpan().SequenceEqual(expected.Xs)
                && candidate.Ys.AsSpan().SequenceEqual(expected.Ys))
            {
                return gid;
            }
        }

        return -1;
    }

    private static int OriginalFootprint(SfntFont font, params string[] tags)
    {
        var total = 0;
        Assert.True(font.TryGetTable("glyf", out var glyf));
        total += glyf.Length;
        foreach (var tag in tags)
        {
            Assert.True(font.TryGetTable(tag, out var table), $"fixture must have {tag}");
            total += table.Length;
        }

        return total;
    }

    private static ushort ReadUInt16(byte[] d, int o) => (ushort)((d[o] << 8) | d[o + 1]);

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
