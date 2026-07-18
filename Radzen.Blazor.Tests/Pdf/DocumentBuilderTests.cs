#nullable enable
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class DocumentBuilderTests
{
    [Fact]
    public void Section_PageDefaults()
    {
        var s = new DocumentBuilder().Sections.Add();
        Assert.Equal(PageSizes.A4, s.PageSize);
        Assert.Equal(PageOrientation.Portrait, s.Orientation);
        Assert.Equal(FlowDirection.LeftToRight, s.Direction);
        Assert.Equal(WritingMode.HorizontalTopToBottom, s.WritingMode);
    }

    [Fact]
    public void Section_MarginsDefaultTwoPointFiveCentimeters()
    {
        var s = new DocumentBuilder().Sections.Add();
        var expected = Unit.FromCentimeter(2.5).Point;
        Assert.Equal(expected, s.Margins.Top.Point, 9);
        Assert.Equal(expected, s.Margins.Right.Point, 9);
        Assert.Equal(expected, s.Margins.Bottom.Point, 9);
        Assert.Equal(expected, s.Margins.Left.Point, 9);
    }

    [Fact]
    public void Section_MarginConvenienceSetsAllEdges()
    {
        var s = new DocumentBuilder().Sections.Add();
        s.Margin = Unit.FromPoint(18);
        Assert.Equal(18, s.Margins.Top.Point, 9);
        Assert.Equal(18, s.Margins.Right.Point, 9);
        Assert.Equal(18, s.Margins.Bottom.Point, 9);
        Assert.Equal(18, s.Margins.Left.Point, 9);
    }
}
