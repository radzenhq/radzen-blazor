#nullable enable
using System;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class UnitTests
{
    private const double CmToPt = 28.346456692913385;
    private const double MmToPt = 2.8346456692913385;

    [Fact]
    public void FromPoint_ExposesPointValue()
    {
        Assert.Equal(72, Unit.FromPoint(72).Point, 9);
    }

    [Fact]
    public void FromInch_Is72Points()
    {
        Assert.Equal(72, Unit.FromInch(1).Point, 9);
        Assert.Equal(144, Unit.FromInch(2).Point, 9);
    }

    [Fact]
    public void FromCentimeter_UsesExactFactor()
    {
        Assert.Equal(CmToPt, Unit.FromCentimeter(1).Point, 12);
        Assert.Equal(CmToPt * 2.54, Unit.FromCentimeter(2.54).Point, 9);
    }

    [Fact]
    public void FromMillimeter_UsesExactFactor()
    {
        Assert.Equal(MmToPt, Unit.FromMillimeter(1).Point, 12);
        Assert.Equal(CmToPt, Unit.FromMillimeter(10).Point, 9);
    }

    [Fact]
    public void ImplicitFromDouble_IsPoints()
    {
        Unit u = 12.5;
        Assert.Equal(12.5, u.Point, 9);
    }

    [Fact]
    public void Equality_Operators()
    {
        var a = Unit.FromPoint(10);
        var b = Unit.FromPoint(10);
        var c = Unit.FromPoint(11);

        Assert.True(a == b);
        Assert.False(a == c);
        Assert.True(a != c);
        Assert.False(a != b);
        Assert.True(a.Equals(b));
    }

    [Fact]
    public void GetHashCode_ConsistentForEqualValues()
    {
        Assert.Equal(Unit.FromPoint(7.25).GetHashCode(), Unit.FromInch(7.25 / 72.0).GetHashCode());
    }

    [Fact]
    public void Addition_And_Subtraction()
    {
        Assert.Equal(7, (Unit.FromPoint(3) + Unit.FromPoint(4)).Point, 9);
        Assert.Equal(1, (Unit.FromPoint(4) - Unit.FromPoint(3)).Point, 9);
    }

    [Fact]
    public void Parse_MapsUnitsToPoints()
    {
        Assert.Equal(CmToPt * 9, Unit.Parse("9cm").Point, 9);
        Assert.Equal(MmToPt * 5, Unit.Parse("5mm").Point, 9);
        Assert.Equal(72, Unit.Parse("1in").Point, 9);
        Assert.Equal(12, Unit.Parse("12pt").Point, 9);
    }

    [Fact]
    public void Parse_BareNumber_IsPoints()
    {
        Assert.Equal(12, Unit.Parse("12").Point, 9);
        Assert.Equal(3.5, Unit.Parse(" 3.5 ").Point, 9);
    }

    [Fact]
    public void Parse_IsCultureInvariant()
    {
        Assert.Equal(1.5, Unit.Parse("1.5pt").Point, 9);
    }

    [Fact]
    public void ImplicitFromString_Parses()
    {
        Unit u = "1in";
        Assert.Equal(72, u.Point, 9);
    }

    [Fact]
    public void Parse_InvalidText_Throws()
    {
        Assert.Throws<FormatException>(() => Unit.Parse("abc"));
    }

    [Fact]
    public void Comparison_Operators()
    {
        var small = Unit.FromPoint(3);
        var big = Unit.FromPoint(4);

        Assert.True(small < big);
        Assert.True(big > small);
        Assert.True(small <= Unit.FromPoint(3));
        Assert.True(big >= Unit.FromPoint(4));
        Assert.False(big < small);
    }
}
