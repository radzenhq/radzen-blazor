using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class SelectionTests
{
    readonly Sheet sheet = new(4, 4);

    [Fact]
    public void Should_SelectSingleCell()
    {
        sheet.Selection.Select(CellRef.Parse("A1"));

        Assert.Equal("A1", sheet.Selection.Cell.ToString());
        Assert.Equal("A1:A1", sheet.Selection.Range.ToString());
    }

    [Fact]
    public void Should_SelectRangeOfCells()
    {
        sheet.Selection.Select(RangeRef.Parse("A1:C3"));

        Assert.Equal("A1", sheet.Selection.Cell.ToString());
        Assert.Equal("A1", sheet.Selection.Range.Start.ToString());
        Assert.Equal("C3", sheet.Selection.Range.End.ToString());
        Assert.Equal("A1:C3", sheet.Selection.Range.ToString());
    }

    [Fact]
    public void Should_SelectTheWholeMergedCellAsPartOfRange()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(RangeRef.Parse("A1:B1"));

        Assert.Equal("A1:C1", sheet.Selection.Range.ToString());
    }

    [Fact]
    public void Should_SelectTheWholeMergedCellByStart()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("B1"));

        Assert.Equal("B1:C1", sheet.Selection.Range.ToString());
    }

    [Fact]
    public void Should_SelectTheWholeMergedCellByEnd()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("C1"));

        Assert.Equal("B1:C1", sheet.Selection.Range.ToString());
    }

    [Fact]
    public void Should_SelectTheWholeMergedCellByEndAndRange()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("C1"), RangeRef.Parse("B1:C1"));

        Assert.Equal("B1:C1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Should_SelectCompleteMergedCellRange()
    {
        sheet.MergedCells.Add(RangeRef.Parse("A1:D1"));
        sheet.Selection.Select(RangeRef.Parse("A1:B2"));

        Assert.Equal("A1:D2", sheet.Selection.Range.ToString());
        Assert.Equal("A1", sheet.Selection.Cell.ToString());
    }
}