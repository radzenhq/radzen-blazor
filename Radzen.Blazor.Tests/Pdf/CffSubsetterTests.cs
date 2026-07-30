#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Xunit;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents.Pdf.Fonts.Cff;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class CffSubsetterTests
{
    private static readonly int[] Requested = [1, 34, 66, 190, 300, 657];
    private const int ClosureCount = 7;

    private static byte[] OriginalCffBytes()
    {
        var sfnt = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/NotoSansSC-Subset.otf"));
        Assert.True(sfnt.TryGetTable("CFF ", out var cffTable));
        return cffTable;
    }

    private static CffFont OriginalFont() => CffFont.Parse(OriginalCffBytes());

    private static CffFont SubsetAndReparse(params int[] glyphIds)
        => CffFont.Parse(CffSubsetter.Subset(OriginalFont(), glyphIds));

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

        AssertIdentityCharset(subset);
    }

    [Fact]
    public void Subset_NotdefStaysAtGid0()
    {
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
    public void Parse_FdSelectOutsideFdArrayThrowsInvalidFontError()
    {
        var bytes = CffSubsetter.Subset(OriginalFont(), [34]);
        var name = CffIndex.Read(bytes, bytes[2]);
        var top = CffIndex.Read(bytes, name.EndOffset);
        var dictionary = CffDict.Parse(top.GetBytes(0));
        var offset = (int)dictionary[1237][0];
        Assert.Equal(0, bytes[offset]);
        bytes[offset + 1] = byte.MaxValue;

        Assert.Throws<InvalidDataException>(() => CffFont.Parse(bytes));
    }

    [Fact]
    public void Subset_ProducesValidParseableCff()
    {
        var bytes = CffSubsetter.Subset(OriginalFont(), Requested);

        Assert.NotNull(bytes);
        Assert.True(bytes.Length > 4);
        Assert.Equal(1, bytes[0]);

        var reparsed = CffFont.Parse(bytes);
        Assert.Equal(ClosureCount, reparsed.GlyphCount);
    }

    [Fact]
    public void Subset_SmallerThanOriginalCff()
    {
        var original = OriginalCffBytes();
        var subset = CffSubsetter.Subset(CffFont.Parse(original), Requested);

        Assert.True(subset.Length < original.Length,
            $"subset {subset.Length} bytes should be smaller than original {original.Length}");
    }

    [Fact]
    public void Subset_RepeatedGlyphIds_CollapseToClosure()
    {
        var subset = CffFont.Parse(CffSubsetter.Subset(OriginalFont(), [34, 34, 66, 66, 66]));

        Assert.Equal(3, subset.GlyphCount);
        AssertIdentityCharset(subset);
    }
}
