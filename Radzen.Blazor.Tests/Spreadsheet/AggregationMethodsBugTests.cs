using System.Collections.Generic;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class AggregationMethodsBugTests
{
    [Fact]
    public void Median_ShouldNotMutateInputList()
    {
        var items = new List<double> { 3, 1, 2 };
        var result = AggregationMethods.Median(items);

        Assert.Equal(2d, result.Value);

        // The input list should be unchanged
        Assert.Equal(3d, items[0]);
        Assert.Equal(1d, items[1]);
        Assert.Equal(2d, items[2]);
    }

    [Fact]
    public void Large_ShouldNotMutateInputList()
    {
        var items = new List<double> { 3, 1, 2 };
        var result = AggregationMethods.Large(items, 1);

        Assert.Equal(3d, result.Value);

        // The input list should be unchanged
        Assert.Equal(3d, items[0]);
        Assert.Equal(1d, items[1]);
        Assert.Equal(2d, items[2]);
    }

    [Fact]
    public void Small_ShouldNotMutateInputList()
    {
        var items = new List<double> { 3, 1, 2 };
        var result = AggregationMethods.Small(items, 1);

        Assert.Equal(1d, result.Value);

        // The input list should be unchanged
        Assert.Equal(3d, items[0]);
        Assert.Equal(1d, items[1]);
        Assert.Equal(2d, items[2]);
    }

    [Fact]
    public void Median_ShouldReturnCorrectValue_ForEvenCount()
    {
        var items = new List<double> { 4, 1, 3, 2 };
        var result = AggregationMethods.Median(items);

        Assert.Equal(2.5, result.Value);
    }

    [Fact]
    public void Large_ShouldReturnKthLargest()
    {
        var items = new List<double> { 5, 3, 1, 4, 2 };
        Assert.Equal(4d, AggregationMethods.Large(items, 2).Value);
    }

    [Fact]
    public void Small_ShouldReturnKthSmallest()
    {
        var items = new List<double> { 5, 3, 1, 4, 2 };
        Assert.Equal(2d, AggregationMethods.Small(items, 2).Value);
    }

    [Fact]
    public void Median_ShouldReturnNumErrorForEmptyList()
    {
        var items = new List<double>();
        var result = AggregationMethods.Median(items);

        Assert.True(result.IsError);
        Assert.Equal(CellError.Num, result.Value);
    }
}
