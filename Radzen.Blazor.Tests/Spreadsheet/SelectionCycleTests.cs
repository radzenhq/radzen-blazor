using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class SelectionCycleTests
{
    readonly Sheet sheet = new(4, 4);

    [Fact]
    public void Should_MoveToTheNextHorizontalCell()
    {
        sheet.Selection.Select(RangeRef.Parse("A1:C1"));

        var cell = sheet.Selection.Cycle(0, 1);

        Assert.Equal("B1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("A1:C1"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_MoveToTheFirstCellInTheRangeWhenAtLastColumn()
    {
        sheet.Selection.Select(CellRef.Parse("C1"), RangeRef.Parse("A1:C1"));

        var cell = sheet.Selection.Cycle(0, 1);

        Assert.Equal("A1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("A1:C1"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_MoveToTheNextRowInTheRangeWhenAtLastColumn()
    {
        sheet.Selection.Select(CellRef.Parse("C1"), RangeRef.Parse("A1:C2"));

        var cell = sheet.Selection.Cycle(0, 1);

        Assert.Equal("A2", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("A1:C2"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_MoveToTheNextVerticalCell()
    {
        sheet.Selection.Select(RangeRef.Parse("A1:A3"));

        var cell = sheet.Selection.Cycle(1, 0);

        Assert.Equal("A2", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("A1:A3"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_MoveToTheFirstCellInTheRangeWhenAtLastRow()
    {
        sheet.Selection.Select(CellRef.Parse("A3"), RangeRef.Parse("A1:A3"));

        var cell = sheet.Selection.Cycle(1, 0);

        Assert.Equal("A1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("A1:A3"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_MoveToTheNextColumnInTheRangeWhenAtLastRow()
    {
        sheet.Selection.Select(CellRef.Parse("A3"), RangeRef.Parse("A1:B3"));

        var cell = sheet.Selection.Cycle(1, 0);

        Assert.Equal("B1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("A1:B3"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_MoveToThePreviousHorizontalCell()
    {
        sheet.Selection.Select(RangeRef.Parse("B1:C1"));

        var cell = sheet.Selection.Cycle(0, -1);

        Assert.Equal("C1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("B1:C1"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_MoveToTheLastCellInTheRangeWhenAtFirstColumn()
    {
        sheet.Selection.Select(CellRef.Parse("A1"), RangeRef.Parse("A1:C1"));

        var cell = sheet.Selection.Cycle(0, -1);

        Assert.Equal("C1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("A1:C1"), sheet.Selection.Range);
    }


    [Fact]
    public void Should_MoveToThePreviousRowInTheRangeWhenAtFirstColumn()
    {
        sheet.Selection.Select(CellRef.Parse("A1"), RangeRef.Parse("A1:C2"));

        var cell = sheet.Selection.Cycle(0, -1);

        Assert.Equal("C2", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("A1:C2"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_MoveToTheNextHorizontalCellIfOnlyOneCellIsSelected()
    {
        sheet.Selection.Select(CellRef.Parse("A1"));

        var cell = sheet.Selection.Cycle(0, 1);

        Assert.Equal("B1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("B1:B1"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_MoveToTheNextVerticalCellIfOnlyOneCellIsSelected()
    {
        sheet.Selection.Select(CellRef.Parse("A1"));

        var cell = sheet.Selection.Cycle(1, 0);

        Assert.Equal("A2", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("A2:A2"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_MoveToThePreviousHorizontalCellIfOnlyOneCellIsSelected()
    {
        sheet.Selection.Select(CellRef.Parse("B1"));

        var cell = sheet.Selection.Cycle(0, -1);

        Assert.Equal("A1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("A1:A1"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_MoveToThePreviousVerticalCellIfOnlyOneCellIsSelected()
    {
        sheet.Selection.Select(CellRef.Parse("A2"));

        var cell = sheet.Selection.Cycle(-1, 0);

        Assert.Equal("A1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("A1:A1"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_StayInTheSameCellIfTheFirstCellIsSelectedAndMovingBackwards()
    {
        sheet.Selection.Select(CellRef.Parse("A1"));

        var cell = sheet.Selection.Cycle(0, -1);

        Assert.Equal("A1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("A1:A1"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_StayInTheSameCellIfTheLastCellIsSelectedAndMovingForwards()
    {
        sheet.Selection.Select(CellRef.Parse("D4"));

        var cell = sheet.Selection.Cycle(0, 1);

        Assert.Equal("D4", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("D4:D4"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_StayInTheSameCellIfOnlyOneCellIsSelectedAndMovingUp()
    {
        sheet.Selection.Select(CellRef.Parse("A1"));

        var cell = sheet.Selection.Cycle(-1, 0);

        Assert.Equal("A1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("A1:A1"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_StayInTheSameCellIfOnlyOneCellIsSelectedAndMovingDown()
    {
        sheet.Selection.Select(CellRef.Parse("D4"));

        var cell = sheet.Selection.Cycle(1, 0);

        Assert.Equal("D4", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("D4:D4"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_GoToTheFirstCellInTheSheetIfNoSelection()
    {
        var cell = sheet.Selection.Cycle(0, 1);

        Assert.Equal("A1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("A1:A1"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_GoToTheNextHorizontalMergedCellInTheSelectedRange()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(RangeRef.Parse("A1:D1"));

        var cell = sheet.Selection.Cycle(0, 1);

        Assert.Equal("B1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("A1:D1"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_GoToThePreviousHorizontalMergedCellInTheSelectedRange()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("D1"), RangeRef.Parse("A1:D1"));

        var cell = sheet.Selection.Cycle(0, -1);

        Assert.Equal("B1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("A1:D1"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_GoToTheNextVerticalMergedCellInTheSelectedRange()
    {
        sheet.MergedCells.Add(RangeRef.Parse("A2:A3"));
        sheet.Selection.Select(RangeRef.Parse("A1:A4"));

        var cell = sheet.Selection.Cycle(1, 0);

        Assert.Equal("A2", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("A1:A4"), sheet.Selection.Range);
    }
    [Fact]
    public void Should_GoToThePreviousVerticalMergedCellInTheSelectedRange()
    {
        sheet.MergedCells.Add(RangeRef.Parse("A2:A3"));
        sheet.Selection.Select(CellRef.Parse("A4"), RangeRef.Parse("A1:A4"));

        var cell = sheet.Selection.Cycle(-1, 0);

        Assert.Equal("A2", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("A1:A4"), sheet.Selection.Range);
    }

    [Fact]
    public void Should_MoveFromMergedCellToNextCell()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("B1"));

        var cell = sheet.Selection.Cycle(0, 1);

        Assert.Equal("D1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
        Assert.Equal(RangeRef.Parse("D1:D1"), sheet.Selection.Range);
    }
}