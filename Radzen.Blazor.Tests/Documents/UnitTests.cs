#nullable enable
using System;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Documents.Tests;

public class UnitTests
{
    private const double CmToPt = 28.346456692913385;
    private const double MmToPt = 2.8346456692913385;

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
    public void ParseReadsAnInchMeasurement()
    {
        var u = Unit.Parse("1in");
        Assert.Equal(72, u.Point, 9);
    }

    [Fact]
    public void Parse_InvalidText_Throws()
    {
        Assert.Throws<FormatException>(() => Unit.Parse("abc"));
    }

    [Fact]
    public void Parse_PercentIsRelative()
    {
        var unit = Unit.Parse("50%");

        Assert.True(unit.IsRelative);
        Assert.Equal(50, unit.Percent, 9);
    }

    [Fact]
    public void Resolve_RelativeIsFractionOfReference()
    {
        Assert.Equal(0.35 * 200, Unit.FromPercent(35).Resolve(200));
        Assert.Equal(0.5 * 123.456, Unit.FromPercent(50).Resolve(123.456));
    }

    [Fact]
    public void Resolve_AbsoluteIgnoresReference()
    {
        Assert.Equal(12, Unit.FromPoint(12).Resolve(500));
        Assert.Equal(72, Unit.Parse("1in").Resolve(500));
    }

    [Fact]
    public void Point_OnRelative_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Unit.FromPercent(50).Point);
    }

    [Fact]
    public void Percent_OnAbsolute_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Unit.FromPoint(50).Percent);
    }

    [Fact]
    public void Relative_And_Absolute_AreNotEqual()
    {
        Assert.NotEqual(Unit.FromPercent(50), Unit.FromPoint(50));
    }

    [Fact]
    public void MixingRelativeAndAbsolute_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Unit.FromPercent(50) + Unit.FromPoint(1));
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
