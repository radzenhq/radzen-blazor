using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class SelectionMovingTests
{
    readonly Sheet sheet = new(4, 4);

    [Fact]
    public void Should_MoveToTheNextHorizontalCell()
    {
        sheet.Selection.Select(CellRef.Parse("A1"));

        var cell = sheet.Selection.Move(0, 1);

        Assert.Equal("B1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
    }

    [Fact]
    public void Should_NotMoveToNextHorizontalCellWhenAlreadyAtLastColumn()
    {
        sheet.Selection.Select(CellRef.Parse("D1"));

        var cell = sheet.Selection.Move(0, 1);

        Assert.Equal("D1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
    }

    [Fact]
    public void Should_MoveToTheNextVerticalCell()
    {
        sheet.Selection.Select(CellRef.Parse("A1"));

        var cell = sheet.Selection.Move(1, 0);

        Assert.Equal("A2", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
    }

    [Fact]
    public void Should_NotMoveToNextVerticalCellWhenAlreadyAtLastRow()
    {
        sheet.Selection.Select(CellRef.Parse("A4"));

        var cell = sheet.Selection.Move(1, 0);

        Assert.Equal("A4", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
    }

    [Fact]
    public void Should_MoveToThePreviousHorizontalCell()
    {
        sheet.Selection.Select(CellRef.Parse("B1"));

        var cell = sheet.Selection.Move(0, -1);

        Assert.Equal("A1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
    }

    [Fact]
    public void Should_NotMoveToPreviousHorizontalCellWhenAlreadyAtFirstColumn()
    {
        sheet.Selection.Select(CellRef.Parse("A1"));

        var cell = sheet.Selection.Move(0, -1);

        Assert.Equal("A1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
    }

    [Fact]
    public void Should_MoveToThePreviousVerticalCell()
    {
        sheet.Selection.Select(CellRef.Parse("A2"));

        var cell = sheet.Selection.Move(-1, 0);

        Assert.Equal("A1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
    }

    [Fact]
    public void Should_NotMoveToPreviousVerticalCellWhenAlreadyAtFirstRow()
    {
        sheet.Selection.Select(CellRef.Parse("A1"));

        var cell = sheet.Selection.Move(-1, 0);

        Assert.Equal("A1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
    }

    [Fact]
    public void Should_MoveToNextHorizontalCellAfterMergedCell()
    {
        sheet.MergedCells.Add(RangeRef.Parse("A1:B1"));
        sheet.Selection.Select(CellRef.Parse("A1"));

        var cell = sheet.Selection.Move(0, 1);

        Assert.Equal("C1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
    }

    [Fact]
    public void Should_MoveToNextVerticalCellAfterMergedCell()
    {
        sheet.MergedCells.Add(RangeRef.Parse("A1:A2"));
        sheet.Selection.Select(CellRef.Parse("A1"));

        var cell = sheet.Selection.Move(1, 0);

        Assert.Equal("A3", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
    }

    [Fact]
    public void Should_MoveToPreviousHorizontalCellBeforeMergedCell()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("B1"));

        var cell = sheet.Selection.Move(0, -1);

        Assert.Equal("A1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
    }

    [Fact]
    public void Should_MoveToPreviousVerticalCellBeforeMergedCell()
    {
        sheet.MergedCells.Add(RangeRef.Parse("A2:A3"));
        sheet.Selection.Select(CellRef.Parse("A2"));

        var cell = sheet.Selection.Move(-1, 0);

        Assert.Equal("A1", cell.ToString());
        Assert.Equal(sheet.Selection.Cell, cell);
    }

    [Fact]
    public void Should_MoveTheSelectedCellOnly()
    {
        sheet.Selection.Select(RangeRef.Parse("A1:C1"));

        var cell = sheet.Selection.Move(0, 1);

        Assert.Equal("B1", cell.ToString());
        Assert.Equal("B1:B1", sheet.Selection.Range.ToString());
    }
}