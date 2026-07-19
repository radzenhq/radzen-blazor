#nullable enable
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;


public class ValueTypeTests
{
    private const double MmToPt = 2.8346456692913385;

    [Fact]
    public void PageSizes_A4_FromMillimeters()
    {
        Assert.Equal(210 * MmToPt, PageSizes.A4.Width.Point, 9);
        Assert.Equal(297 * MmToPt, PageSizes.A4.Height.Point, 9);
        Assert.Equal(595.2755905511812, PageSizes.A4.Width.Point, 9);
        Assert.Equal(841.8897637795277, PageSizes.A4.Height.Point, 9);
    }

    [Fact]
    public void PageSizes_Letter_IsExactPoints()
    {
        Assert.Equal(612, PageSizes.Letter.Width.Point, 9);
        Assert.Equal(792, PageSizes.Letter.Height.Point, 9);
    }

    [Fact]
    public void PageSizes_Legal_IsExactPoints()
    {
        Assert.Equal(612, PageSizes.Legal.Width.Point, 9);
        Assert.Equal(1008, PageSizes.Legal.Height.Point, 9);
    }

    [Fact]
    public void PageSizes_A3_A5_FromMillimeters()
    {
        Assert.Equal(297 * MmToPt, PageSizes.A3.Width.Point, 9);
        Assert.Equal(420 * MmToPt, PageSizes.A3.Height.Point, 9);
        Assert.Equal(148 * MmToPt, PageSizes.A5.Width.Point, 9);
        Assert.Equal(210 * MmToPt, PageSizes.A5.Height.Point, 9);
    }

}
