using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class SelectionMergingTests
{
    readonly Sheet sheet = new(4, 4);

    [Fact]
    public void Should_SelectAllHorizontalCellsBetweenTheCurrentCellAndTheNewOne()
    {
        sheet.Selection.Select(CellRef.Parse("A1"));
        sheet.Selection.Merge(CellRef.Parse("C1"));

        Assert.Equal("A1:C1", sheet.Selection.Range.ToString());
        Assert.Equal("A1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Should_SelectAllVerticalCellsBetweenTheCurrentCellAndTheNewOne()
    {
        sheet.Selection.Select(CellRef.Parse("A1"));
        sheet.Selection.Merge(CellRef.Parse("A3"));

        Assert.Equal("A1:A3", sheet.Selection.Range.ToString());
        Assert.Equal("A1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Should_ExtendRangeWhenMerginANewCell()
    {
        sheet.Selection.Select(RangeRef.Parse("A1:C1"));
        sheet.Selection.Merge(CellRef.Parse("D2"));

        Assert.Equal("A1:D2", sheet.Selection.Range.ToString());
        Assert.Equal("A1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Should_AddTheEntireMergedCell()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("A1"));
        sheet.Selection.Merge(CellRef.Parse("B1"));

        Assert.Equal("A1:C1", sheet.Selection.Range.ToString());
        Assert.Equal("A1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Should_AddTheEntireMergedCellWhenSelectingIt()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("B1"));
        sheet.Selection.Merge(CellRef.Parse("A1"));

        Assert.Equal("A1:C1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Should_ExpandSelectionToIncludeMergedCells()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("A1"));
        sheet.Selection.Merge(CellRef.Parse("B2"));

        Assert.Equal("A1:C2", sheet.Selection.Range.ToString());
        Assert.Equal("A1", sheet.Selection.Cell.ToString());
    }
}