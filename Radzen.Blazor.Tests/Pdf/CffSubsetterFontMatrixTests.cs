#nullable enable
using System.Collections.Generic;
using Radzen.Documents.Pdf.Fonts.Cff;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class CffSubsetterFontMatrixTests
{
    private static readonly byte[] RealHalfThousandth = [30, 0x0A, 0x00, 0x05, 0xFF];

    private static byte[] SourceWithFontMatrix()
    {
        byte[] fontMatrix =
        [
            .. RealHalfThousandth, 0x8B, 0x8B, .. RealHalfThousandth, 0x8B, 0x8B, 12, 7,
        ];

        byte[] privateDict = [28, 0x01, 0xF4, 20, 0x8B, 21];
        byte[][] charStrings = [[0x0E], [0x0E]];
        return CffFixtureBuilder.Build(privateDict, charStrings, fontMatrix);
    }

    private static Dictionary<int, double[]> SubsetTopDict()
    {
        var source = CffFont.Parse(SourceWithFontMatrix());
        var subset = CffSubsetter.Subset(source, [1]);

        var nameIndex = CffIndex.Read(subset, subset[2]);
        var topDictIndex = CffIndex.Read(subset, nameIndex.EndOffset);
        return CffDict.Parse(topDictIndex.GetBytes(0));
    }

    [Fact]
    public void Subset_CopiesFontMatrixIntoRebuiltTopDict()
    {
        var topDict = SubsetTopDict();

        Assert.True(topDict.ContainsKey(1207), "Rebuilt Top DICT is missing FontMatrix (12 7).");
    }

    [Fact]
    public void Subset_FontMatrixValuesMatchSource()
    {
        var topDict = SubsetTopDict();

        Assert.True(topDict.TryGetValue(1207, out var matrix));
        Assert.Equal(6, matrix!.Length);
        Assert.Equal(0.0005, matrix[0], 9);
        Assert.Equal(0.0, matrix[1], 9);
        Assert.Equal(0.0, matrix[2], 9);
        Assert.Equal(0.0005, matrix[3], 9);
        Assert.Equal(0.0, matrix[4], 9);
        Assert.Equal(0.0, matrix[5], 9);
    }

    [Fact]
    public void Subset_WithFontMatrix_StillReparsesWithWidths()
    {
        var source = CffFont.Parse(SourceWithFontMatrix());
        var subset = CffFont.Parse(CffSubsetter.Subset(source, [1]));

        Assert.Equal(2, subset.GlyphCount);
        Assert.Equal(500, subset.GetAdvanceWidth(1));
    }

    [Fact]
    public void Subset_DoesNotAlsoWriteFontMatrixIntoFdDict()
    {
        var subset = CffFont.Parse(CffSubsetter.Subset(CffFont.Parse(SourceWithFontMatrix()), [1]));

        Assert.NotNull(subset.FontMatrix);
        Assert.Null(subset.GetFdFontMatrix(0));
    }
}
