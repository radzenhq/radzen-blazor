#nullable enable
using System;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class UnitPublicContractTests
{
    [Theory]
    [InlineData("12pt", 12)]
    [InlineData("2.54cm", 72)]
    [InlineData("25.4mm", 72)]
    [InlineData("1in", 72)]
    [InlineData("12", 12)]
    [InlineData(" 3.5 PT ", 3.5)]
    public void Parse_AbsoluteFormsResolveToPoints(string text, double expected)
        => Assert.Equal(expected, Unit.Parse(text).Point, 9);

    [Theory]
    [InlineData("50%", 50)]
    [InlineData(" 12.5 % ", 12.5)]
    [InlineData("-25%", -25)]
    public void Parse_PercentFormsRemainRelative(string text, double expected)
    {
        var unit = Unit.Parse(text);

        Assert.True(unit.IsRelative);
        Assert.Equal(expected, unit.Percent, 9);
    }

    [Fact]
    public void AbsoluteFactoriesUseTypographicPointConversions()
    {
        Assert.Equal(72, Unit.FromInch(1).Point, 9);
        Assert.Equal(72, Unit.FromCentimeter(2.54).Point, 9);
        Assert.Equal(72, Unit.FromMillimeter(25.4).Point, 9);
        Assert.Equal(12, Unit.FromPoint(12).Point, 9);
        Assert.Equal(12, Unit.FromDouble(12).Point, 9);
        Assert.Equal(12, Unit.FromString("12pt").Point, 9);
    }

    [Fact]
    public void PointAndPercentRejectTheOppositeKind()
    {
        Assert.Throws<InvalidOperationException>(() => Unit.FromPercent(50).Point);
        Assert.Throws<InvalidOperationException>(() => Unit.FromPoint(50).Percent);
    }

    [Fact]
    public void ResolveUsesTheReferenceOnlyForRelativeUnits()
    {
        Assert.Equal(75, Unit.FromPercent(25).Resolve(300), 9);
        Assert.Equal(25, Unit.FromPoint(25).Resolve(300), 9);
    }

    [Fact]
    public void AbsoluteArithmeticPreservesPointValues()
    {
        Assert.Equal(9, (Unit.FromPoint(4) + Unit.FromPoint(5)).Point, 9);
        Assert.Equal(1, Unit.Subtract(Unit.FromPoint(5), Unit.FromPoint(4)).Point, 9);
        Assert.Equal(9, Unit.Add(Unit.FromPoint(4), Unit.FromPoint(5)).Point, 9);
    }

    [Fact]
    public void RelativeArithmeticPreservesPercentValues()
    {
        Assert.Equal(75, (Unit.FromPercent(50) + Unit.FromPercent(25)).Percent, 9);
        Assert.Equal(25, (Unit.FromPercent(50) - Unit.FromPercent(25)).Percent, 9);
    }

    [Fact]
    public void MixedKindArithmeticThrows()
    {
        var absolute = Unit.FromPoint(10);
        var relative = Unit.FromPercent(10);

        Assert.Throws<InvalidOperationException>(() => absolute + relative);
        Assert.Throws<InvalidOperationException>(() => relative + absolute);
        Assert.Throws<InvalidOperationException>(() => absolute - relative);
        Assert.Throws<InvalidOperationException>(() => Unit.Subtract(relative, absolute));
    }

    [Fact]
    public void EqualityIncludesTheMeasurementKind()
    {
        var first = Unit.FromPoint(10);
        var equal = Unit.Parse("10pt");
        var relative = Unit.FromPercent(10);

        Assert.True(first == equal);
        Assert.False(first != equal);
        Assert.Equal(first.GetHashCode(), equal.GetHashCode());
        Assert.NotEqual(first, relative);
    }

    [Fact]
    public void ComparisonOperatorsOrderValuesOfTheSameKind()
    {
        var small = Unit.FromPoint(3);
        var large = Unit.FromPoint(4);

        Assert.True(small < large);
        Assert.True(small <= large);
        Assert.True(large > small);
        Assert.True(large >= small);
        Assert.Equal(0, small.CompareTo(Unit.FromPoint(3)));
    }

    [Fact]
    public void MixedKindComparisonThrows()
    {
        var absolute = Unit.FromPoint(1000);
        var relative = Unit.FromPercent(-1000);

        Assert.Throws<InvalidOperationException>(() => absolute < relative);
        Assert.Throws<InvalidOperationException>(() => absolute <= relative);
        Assert.Throws<InvalidOperationException>(() => relative > absolute);
        Assert.Throws<InvalidOperationException>(() => relative >= absolute);
        Assert.Throws<InvalidOperationException>(() => absolute.CompareTo(relative));
    }

    [Fact]
    public void MixedKindEqualityIsFalseAndDoesNotThrow()
    {
        var absolute = Unit.FromPoint(10);
        var relative = Unit.FromPercent(10);

        Assert.False(absolute == relative);
        Assert.True(absolute != relative);
        Assert.False(absolute.Equals(relative));
        Assert.False(absolute.Equals((object)relative));
    }

    [Fact]
    public void ObjectComparisonHandlesNullAndRejectsOtherTypes()
    {
        IComparable unit = Unit.FromPoint(1);

        Assert.Equal(1, unit.CompareTo(null));
        Assert.Throws<ArgumentException>(() => unit.CompareTo(1d));
    }

    [Fact]
    public void ImplicitDoubleConversionCreatesAnAbsoluteUnit()
    {
        Unit unit = 12d;

        Assert.False(unit.IsRelative);
        Assert.Equal(12, unit.Point, 9);
    }

    [Fact]
    public void ImplicitStringConversionUsesTheThrowingParser()
    {
        Assert.Throws<FormatException>(() =>
        {
            Unit unit = "not-a-unit";
            _ = unit;
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("12px")]
    [InlineData("12em")]
    [InlineData("%")]
    [InlineData("12%%")]
    public void Parse_InvalidFormsThrowFormatException(string text)
        => Assert.Throws<FormatException>(() => Unit.Parse(text));

    [Fact]
    public void Parse_NullThrowsArgumentNullException()
        => Assert.Throws<ArgumentNullException>(() => Unit.Parse(null!));

    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    [InlineData("NaN%")]
    [InlineData("Infinity%")]
    [InlineData("-Infinity%")]
    [InlineData("NaNpt")]
    [InlineData("Infinityin")]
    public void Parse_NonFiniteNumbersThrowFormatException(string text)
        => Assert.Throws<FormatException>(() => Unit.Parse(text));

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void Factories_RejectNonFiniteValues(double value)
    {
        Assert.Throws<ArgumentException>(() => Unit.FromPoint(value));
        Assert.Throws<ArgumentException>(() => Unit.FromPercent(value));
        Assert.Throws<ArgumentException>(() => Unit.FromInch(value));
        Assert.Throws<ArgumentException>(() => Unit.FromCentimeter(value));
        Assert.Throws<ArgumentException>(() => Unit.FromMillimeter(value));
        Assert.Throws<ArgumentException>(() => Unit.FromDouble(value));
        Assert.Throws<ArgumentException>(() =>
        {
            Unit unit = value;
            _ = unit;
        });
    }
}
