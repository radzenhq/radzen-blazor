#nullable enable
using System;
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;


public class ValueTypeTests
{
    private const double MmToPt = 2.8346456692913385;

    [Fact]
    public void Rect_DerivedEdges()
    {
        var r = new Rect(10, 20, 100, 40);
        Assert.Equal(10, r.X, 9);
        Assert.Equal(20, r.Y, 9);
        Assert.Equal(100, r.Width, 9);
        Assert.Equal(40, r.Height, 9);
        Assert.Equal(10, r.Left, 9);
        Assert.Equal(20, r.Top, 9);
        Assert.Equal(110, r.Right, 9);
        Assert.Equal(60, r.Bottom, 9);
    }

    [Fact]
    public void Rect_Equality()
    {
        Assert.True(new Rect(1, 2, 3, 4) == new Rect(1, 2, 3, 4));
        Assert.True(new Rect(1, 2, 3, 4) != new Rect(1, 2, 3, 5));
        Assert.Equal(new Rect(1, 2, 3, 4), new Rect(1, 2, 3, 4));
    }

    [Fact]
    public void PageSize_Equality()
    {
        var a = new PageSize(Unit.FromPoint(100), Unit.FromPoint(200));
        var b = new PageSize(Unit.FromPoint(100), Unit.FromPoint(200));
        var c = new PageSize(Unit.FromPoint(100), Unit.FromPoint(201));

        Assert.True(a == b);
        Assert.True(a != c);
        Assert.Equal(a, b);
    }

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

    [Fact]
    public void Font_Defaults()
    {
        var f = new Font();
        Assert.Equal(10, f.Size, 9);
        Assert.False(f.Bold);
        Assert.False(f.Italic);
        Assert.False(f.Underline);
    }

    private static void AssertMembers<TEnum>(params string[] expected) where TEnum : struct, Enum
    {
        var actual = Enum.GetNames(typeof(TEnum)).OrderBy(n => n).ToArray();
        Assert.Equal(expected.OrderBy(n => n).ToArray(), actual);
    }

    [Fact]
    public void HorizontalAlignment_Members()
        => AssertMembers<HorizontalAlignment>("Left", "Center", "Right", "Justify", "Start", "End");

    [Fact]
    public void VerticalAlignment_Members()
        => AssertMembers<VerticalAlignment>("Top", "Middle", "Bottom");

    [Fact]
    public void PageOrientation_Members()
        => AssertMembers<PageOrientation>("Portrait", "Landscape");

    [Fact]
    public void BorderStyle_Members()
        => AssertMembers<BorderStyle>("None", "Solid", "Dashed", "Dotted");

    [Fact]
    public void FlowDirection_Members()
        => AssertMembers<FlowDirection>("LeftToRight", "RightToLeft");

    [Fact]
    public void WritingMode_Members()
        => AssertMembers<WritingMode>("HorizontalTopToBottom", "VerticalRightToLeft", "VerticalLeftToRight");
}
