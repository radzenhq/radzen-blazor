#nullable enable
using Radzen.Documents.Fonts;
using Radzen.Documents.Layout;
using Radzen.Documents;
using Radzen.Blazor.Pdf.Tests;
using Xunit;
using Radzen.Blazor.Tests.Isolated;

namespace Radzen.Blazor.Documents.Tests;

public class LineHeightOracleTests
{
    [Fact]
    public void LiberationSansLineHeightPerEm_IsHheaAscentPlusDescentPlusGapOverUnitsPerEm()
        => Assert.Equal(2355.0 / 2048.0, PaginationSupport.LiberationSansLineHeightPerEm, 12);

    [Fact]
    public void BuiltInLineHeightPerEm_IsTheFlatOnePointTwoFactor()
        => Assert.Equal(1.2, PaginationSupport.BuiltInLineHeightPerEm, 12);

    [Theory]
    [InlineData(8)]
    [InlineData(12)]
    [InlineData(18)]
    public void LineBreaker_MatchesThePinnedLiberationSansLineHeight(double size)
    {
        var fonts = PaginationSupport.Fonts();
        var paragraph = PaginationSupport.Text("Xg", size);

        Assert.Equal(
            PaginationSupport.LineHeight(size),
            IsolatedLineBreaker.Break(paragraph, 100000, fonts)[0].Height,
            9);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.5)]
    [InlineData(2.0)]
    public void LineBreaker_ScalesThePinnedLiberationSansLineHeightByLineSpacing(double spacing)
    {
        var fonts = PaginationSupport.Fonts();
        var paragraph = PaginationSupport.Text("Xg");
        paragraph.LineSpacing = spacing;

        Assert.Equal(
            PaginationSupport.LineHeight(12, spacing),
            IsolatedLineBreaker.Break(paragraph, 100000, fonts)[0].Height,
            9);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(12)]
    [InlineData(24)]
    public void LineBreaker_MatchesThePinnedBuiltInLineHeight(double size)
    {
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("Xg").Font.Size = size;

        Assert.Equal(
            PaginationSupport.BuiltInLineHeight(size),
            IsolatedLineBreaker.Break(paragraph, 100000, new FontCollection())[0].Height,
            9);
    }
}
