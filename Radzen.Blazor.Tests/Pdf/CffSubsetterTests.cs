#nullable enable
using System;
using System.IO;
using Xunit;
using Radzen.Documents.Pdf.Fonts.Sfnt;
using Radzen.Documents.Pdf.Fonts.Cff;

namespace Radzen.Blazor.Pdf.Tests;

// Contract for CffSubsetter.Subset(CffFont, IReadOnlyCollection<int> glyphIds).
// The subsetter rebuilds a compact, embeddable CFF that contains only the closure of
// the requested glyphs: the requested original glyph ids plus glyph 0 (.notdef). CID-keyed
// input yields CID-keyed output. Charstring subrs may be retained wholesale; only glyphs
// (charstrings) are dropped, so the glyph closure is exactly requested-union-{0}.
//
// The oracle is F4a CffFont.Parse: the subset bytes are re-parsed and every expectation is
// checked on the parsed result. Output glyph ordering is treated as opaque - a kept glyph is
// located in the output by searching the charset for its ORIGINAL CID (unique per glyph in an
// Adobe-Identity CID font), then its advance width / FD are read at that output gid.
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
// Not-requested probes: gid 223 -> CID 340, gid 418 -> CID 28904.
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

    // charset[outGid] -> original CID; find the output gid carrying a given original CID.
    private static int OutGidForCid(CffFont cff, int cid)
    {
        var charset = cff.Charset;
        for (var i = 0; i < charset.Length; i++)
        {
            if (charset[i] == cid)
            {
                return i;
            }
        }

        return -1;
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
    public void Subset_Glyph0AlwaysPresent()
    {
        // Glyph 0 is not in the requested set but must be pulled into the closure.
        Assert.DoesNotContain(0, Requested);

        var subset = SubsetAndReparse(Requested);

        Assert.NotEqual(-1, OutGidForCid(subset, 0));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(34)]
    [InlineData(66)]
    [InlineData(307)]
    [InlineData(2341)]
    [InlineData(65456)]
    public void Subset_KeptGlyphsRetainOriginalCid(int cid)
    {
        var subset = SubsetAndReparse(Requested);

        Assert.NotEqual(-1, OutGidForCid(subset, cid));
    }

    [Theory]
    [InlineData(0, 1000)]
    [InlineData(1, 224)]
    [InlineData(34, 608)]
    [InlineData(66, 563)]
    [InlineData(307, 608)]
    [InlineData(2341, 1000)]
    [InlineData(65456, 1000)]
    public void Subset_KeptGlyphsRetainOriginalAdvanceWidth(int cid, int expectedWidth)
    {
        var subset = SubsetAndReparse(Requested);
        var outGid = OutGidForCid(subset, cid);
        Assert.NotEqual(-1, outGid);

        Assert.Equal(expectedWidth, subset.GetAdvanceWidth(outGid));
    }

    // Cross-check the subset advance against the original font at the same CID.
    [Theory]
    [InlineData(1, 1)]
    [InlineData(34, 34)]
    [InlineData(66, 66)]
    [InlineData(190, 307)]
    [InlineData(300, 2341)]
    [InlineData(657, 65456)]
    public void Subset_WidthMatchesOriginalFontAtSameCid(int originalGid, int cid)
    {
        var original = OriginalFont();
        var subset = SubsetAndReparse(Requested);
        var outGid = OutGidForCid(subset, cid);
        Assert.NotEqual(-1, outGid);

        Assert.Equal(original.GetAdvanceWidth(originalGid), subset.GetAdvanceWidth(outGid));
    }

    [Theory]
    [InlineData(340)]   // gid 223, not requested
    [InlineData(28904)] // gid 418, not requested
    public void Subset_NotKeptGlyphIsAbsent(int cid)
    {
        var subset = SubsetAndReparse(Requested);

        Assert.Equal(-1, OutGidForCid(subset, cid));
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
        var subset = SubsetAndReparse(34);

        Assert.Equal(2, subset.GlyphCount);
        Assert.NotEqual(-1, OutGidForCid(subset, 0));
        Assert.NotEqual(-1, OutGidForCid(subset, 34));
        Assert.True(subset.IsCidKeyed);
    }

    [Fact]
    public void Subset_EmptySet_YieldsNotdefOnly()
    {
        var subset = SubsetAndReparse();

        Assert.Equal(1, subset.GlyphCount);
        Assert.NotEqual(-1, OutGidForCid(subset, 0));
        Assert.True(subset.IsCidKeyed);
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
    }
}
