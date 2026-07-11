#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Radzen.Documents.Pdf.Fonts.Cff;

namespace Radzen.Blazor.Pdf.Tests;

// Contract for CffSubsetter.Subset(CffFont, IReadOnlyCollection<int> glyphIds).
// COMPACT CID model (PDF CIDFontType0 with content codes == compact ids): the
// subsetter rebuilds a CID-keyed CFF holding exactly the closure of the requested
// glyphs (requested original gids plus glyph 0) RENUMBERED into a contiguous
// space 0..N-1 with an IDENTITY charset (CID == new gid). Glyph 0 stays .notdef.
// Charstrings are copied verbatim, so a kept glyph is located in the output by
// matching its original charstring bytes; its advance width must survive.
//
// The oracle is CffFont.Parse: the subset bytes are re-parsed and every
// expectation is checked on the parsed result.
//
// Values derived from the exact fixture NotoSansSC-Subset.otf via fontTools 4.60.2
// (font["CFF "].cff, getGlyphOrder, cidNNNNN name -> CID, Type 2 charstring .width):
//   gid 0   -> CID 0     width 1000
//   gid 1   -> CID 1     width 224
//   gid 34  -> CID 34    width 608
//   gid 66  -> CID 66    width 563
//   gid 190 -> CID 307   width 608
//   gid 300 -> CID 2341  width 1000
//   gid 657 -> CID 65456 width 1000
// Not-requested probes: gid 223 and gid 418. The original charset is NOT identity
// above gid ~190, so identity-charset assertions genuinely pin the renumbering.
// This is a CID-keyed CFF (ROS Adobe Identity 0), 658 glyphs, CFF table 59984 bytes.
public class CffSubsetterTests
{
    private static readonly int[] Requested = [1, 34, 66, 190, 300, 657];
    private const int ClosureCount = 7; // Requested (6 distinct) + glyph 0.

    private static byte[] OriginalCffBytes()
    {
        var sfnt = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf"));
        Assert.True(sfnt.TryGetTable("CFF ", out var cffTable));
        return cffTable;
    }

    private static CffFont OriginalFont() => CffFont.Parse(OriginalCffBytes());

    private static CffFont SubsetAndReparse(params int[] glyphIds)
        => CffFont.Parse(CffSubsetter.Subset(OriginalFont(), glyphIds));

    // Charstrings are copied verbatim: the output gids carrying the original
    // glyph's charstring bytes. Duplicates are possible (Latin 'A' gid 34 and
    // Cyrillic А gid 190 share identical charstrings in the fixture).
    private static List<int> OutGidsForOriginal(CffFont subset, CffFont original, int originalGid)
    {
        var expected = original.GetCharStringBytes(originalGid);
        var matches = new List<int>();
        for (var gid = 0; gid < subset.GlyphCount; gid++)
        {
            if (subset.GetCharStringBytes(gid).AsSpan().SequenceEqual(expected))
            {
                matches.Add(gid);
            }
        }

        return matches;
    }

    private static void AssertIdentityCharset(CffFont subset)
    {
        for (var gid = 0; gid < subset.GlyphCount; gid++)
        {
            Assert.Equal(gid, subset.Charset[gid]);
        }
    }

    [Fact]
    public void Subset_ReparsesAsCidKeyed()
    {
        var subset = SubsetAndReparse(Requested);

        Assert.True(subset.IsCidKeyed);
    }

    [Fact]
    public void Subset_PreservesRegistryOrderingSupplement()
    {
        var subset = SubsetAndReparse(Requested);

        Assert.Equal("Adobe", subset.Registry);
        Assert.Equal("Identity", subset.Ordering);
        Assert.Equal(0, subset.Supplement);
    }

    [Fact]
    public void Subset_GlyphCountEqualsClosureSize()
    {
        var subset = SubsetAndReparse(Requested);

        Assert.Equal(ClosureCount, subset.GlyphCount);
    }

    [Fact]
    public void Subset_CharsetIsIdentityOverCompactSpace()
    {
        var subset = SubsetAndReparse(Requested);

        // Original CIDs (307, 2341, 65456, ...) are renumbered away: CID == new gid.
        AssertIdentityCharset(subset);
    }

    [Fact]
    public void Subset_NotdefStaysAtGid0()
    {
        // Glyph 0 is not in the requested set but must be pulled into the closure
        // and keep its position: compact gid 0 carries the original .notdef.
        Assert.DoesNotContain(0, Requested);

        var original = OriginalFont();
        var subset = SubsetAndReparse(Requested);

        Assert.Equal(original.GetCharStringBytes(0), subset.GetCharStringBytes(0));
    }

    [Theory]
    [InlineData(0, 1000)]
    [InlineData(1, 224)]
    [InlineData(34, 608)]
    [InlineData(66, 563)]
    [InlineData(190, 608)]
    [InlineData(300, 1000)]
    [InlineData(657, 1000)]
    public void Subset_KeptGlyphsRetainCharstringAndAdvance(int originalGid, int expectedWidth)
    {
        var original = OriginalFont();
        var subset = SubsetAndReparse(Requested);

        var outGids = OutGidsForOriginal(subset, original, originalGid);
        Assert.NotEmpty(outGids);

        foreach (var outGid in outGids)
        {
            Assert.Equal(expectedWidth, subset.GetAdvanceWidth(outGid));
            Assert.Equal(original.GetAdvanceWidth(originalGid), subset.GetAdvanceWidth(outGid));
        }
    }

    [Theory]
    [InlineData(223)]
    [InlineData(418)]
    public void Subset_NotKeptGlyphIsAbsent(int originalGid)
    {
        var original = OriginalFont();
        var subset = SubsetAndReparse(Requested);

        Assert.Empty(OutGidsForOriginal(subset, original, originalGid));
    }

    [Fact]
    public void Subset_KeptGlyphFdSelectStaysInRange()
    {
        var subset = SubsetAndReparse(Requested);

        Assert.True(subset.FdCount >= 1);
        for (var gid = 0; gid < subset.GlyphCount; gid++)
        {
            Assert.InRange(subset.GetFd(gid), 0, subset.FdCount - 1);
        }
    }

    [Fact]
    public void Subset_SingleGlyph_StillKeepsNotdef()
    {
        var original = OriginalFont();
        var subset = SubsetAndReparse(34);

        Assert.Equal(2, subset.GlyphCount);
        Assert.True(subset.IsCidKeyed);
        AssertIdentityCharset(subset);
        Assert.Equal(original.GetCharStringBytes(0), subset.GetCharStringBytes(0));
        Assert.NotEmpty(OutGidsForOriginal(subset, original, 34));
    }

    [Fact]
    public void Subset_EmptySet_YieldsNotdefOnly()
    {
        var subset = SubsetAndReparse();

        Assert.Equal(1, subset.GlyphCount);
        Assert.True(subset.IsCidKeyed);
        AssertIdentityCharset(subset);
    }

    [Fact]
    public void Subset_IsDeterministic()
    {
        var font = OriginalFont();

        var first = CffSubsetter.Subset(font, Requested);
        var second = CffSubsetter.Subset(font, Requested);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Subset_ProducesValidParseableCff()
    {
        var bytes = CffSubsetter.Subset(OriginalFont(), Requested);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 4);
        Assert.Equal(1, bytes[0]); // CFF major version.

        var reparsed = CffFont.Parse(bytes);
        Assert.Equal(ClosureCount, reparsed.GlyphCount);
    }

    [Fact]
    public void Subset_SmallerThanOriginalCff()
    {
        var original = OriginalCffBytes();
        var subset = CffSubsetter.Subset(CffFont.Parse(original), Requested);

        // Dropping 651 of 658 charstrings must shrink the blob.
        Assert.True(subset.Length < original.Length,
            $"subset {subset.Length} bytes should be smaller than original {original.Length}");
    }

    [Fact]
    public void Subset_RepeatedGlyphIds_CollapseToClosure()
    {
        // Duplicates in the request must not create duplicate output glyphs.
        var subset = CffFont.Parse(CffSubsetter.Subset(OriginalFont(), [34, 34, 66, 66, 66]));

        Assert.Equal(3, subset.GlyphCount); // {0, 34, 66}
        AssertIdentityCharset(subset);
    }
}
