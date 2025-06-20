using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class SelectionExtensionTests
{
    readonly Sheet sheet = new(10, 10);

    [Fact]
    public void Extend_ShouldAppendNextHorizontalCellToRange()
    {
        sheet.Selection.Select(CellRef.Parse("B1"));
        sheet.Selection.Extend(0, 1);

        Assert.Equal("B1:C1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldAppendNextVerticalCellToRange()
    {
        sheet.Selection.Select(CellRef.Parse("B1"));
        sheet.Selection.Extend(1, 0);

        Assert.Equal("B1:B2", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldSubtractPreviousHorizontalCellFromRange()
    {
        sheet.Selection.Select(RangeRef.Parse("B1:C1"));
        sheet.Selection.Extend(0, -1);

        Assert.Equal("B1:B1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldSubtractPreviousVerticalCellFromRange()
    {
        sheet.Selection.Select(RangeRef.Parse("B1:B2"));
        sheet.Selection.Extend(-1, 0);

        Assert.Equal("B1:B1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldAppendPreviousHorizontalCellToRange()
    {
        sheet.Selection.Select(CellRef.Parse("B1"));
        sheet.Selection.Extend(0, -1);

        Assert.Equal("A1:B1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldFirstAppendAndThenSubtractNextHorizontalCell()
    {
        sheet.Selection.Select(CellRef.Parse("B1"));
        sheet.Selection.Extend(0, 1);
        sheet.Selection.Extend(0, -1);

        Assert.Equal("B1:B1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldFirstAppendAndThenSubtractPreviousHorizontalCell()
    {
        sheet.Selection.Select(CellRef.Parse("B1"));
        sheet.Selection.Extend(0, -1);
        sheet.Selection.Extend(0, 1);

        Assert.Equal("B1:B1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldAppendPreviousVerticalCellToRange()
    {
        sheet.Selection.Select(CellRef.Parse("B2"));
        sheet.Selection.Extend(-1, 0);

        Assert.Equal("B1:B2", sheet.Selection.Range.ToString());
        Assert.Equal("B2", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldFirstAppendAndThenSubtractVerticalCell()
    {
        sheet.Selection.Select(CellRef.Parse("B2"));
        sheet.Selection.Extend(1, 0);
        sheet.Selection.Extend(-1, 0);

        Assert.Equal("B2:B2", sheet.Selection.Range.ToString());
        Assert.Equal("B2", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Select_ShouldSelectTheWholeMergedCellRangeWhenTheStartIsUsed()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("B1"));

        Assert.Equal("B1:C1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Select_ShouldSelectTheWholeMergedCellRangeWhenTheEndIsUsed()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("C1"));

        Assert.Equal("B1:C1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldAddNextHorizontalCellAfterMergedCell()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("B1"));
        sheet.Selection.Extend(0, 1);

        Assert.Equal("B1:D1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldAddNextHorizontalCellAfterMergedCellAgain()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("B1"));
        sheet.Selection.Extend(0, 1);
        sheet.Selection.Extend(0, 1);

        Assert.Equal("B1:E1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldAddNextMergedCellAfterMergedCell()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.MergedCells.Add(RangeRef.Parse("D1:E1"));
        sheet.Selection.Select(CellRef.Parse("B1"));
        sheet.Selection.Extend(0, 1);

        Assert.Equal("B1:E1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldSubtractNextMergedCellAfterMergedCell()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.MergedCells.Add(RangeRef.Parse("D1:E1"));
        sheet.Selection.Select(CellRef.Parse("B1"));
        sheet.Selection.Extend(0, 1);
        sheet.Selection.Extend(0, -1);

        Assert.Equal("B1:C1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldAddPreviousMergedCellBeforeMergedCell()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.MergedCells.Add(RangeRef.Parse("D1:E1"));
        sheet.Selection.Select(CellRef.Parse("D1"));
        sheet.Selection.Extend(0, -1);

        Assert.Equal("B1:E1", sheet.Selection.Range.ToString());
        Assert.Equal("D1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldSubtractPreviousMergedCellBeforeMergedCell()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.MergedCells.Add(RangeRef.Parse("D1:E1"));
        sheet.Selection.Select(CellRef.Parse("D1"));
        sheet.Selection.Extend(0, -1);
        sheet.Selection.Extend(0, 1);

        Assert.Equal("D1:E1", sheet.Selection.Range.ToString());
        Assert.Equal("D1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldAddPreviousHorizontalMergedCell()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("D1"));
        sheet.Selection.Extend(0, -1);

        Assert.Equal("B1:D1", sheet.Selection.Range.ToString());
        Assert.Equal("D1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldSubractPreviousMergedCellFromSelection()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("D1"));
        sheet.Selection.Extend(0, -1);

        Assert.Equal("B1:D1", sheet.Selection.Range.ToString());
        sheet.Selection.Extend(0, 1);

        Assert.Equal("D1:D1", sheet.Selection.Range.ToString());
        Assert.Equal("D1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldSubtractNextMergedCellFromSelection()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(RangeRef.Parse("A1:C1"));
        sheet.Selection.Extend(0, -1);

        Assert.Equal("A1:A1", sheet.Selection.Range.ToString());
        Assert.Equal("A1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldSubtractPreviousHorizontalCellBeforeMergedCellFromSelection()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(RangeRef.Parse("B1:C1"));
        sheet.Selection.Extend(0, -1);
        Assert.Equal("A1:C1", sheet.Selection.Range.ToString());

        sheet.Selection.Extend(0, 1);

        Assert.Equal("B1:C1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldAddPreviousHorizontalCellBeforeMergedCell()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("B1"));
        sheet.Selection.Extend(0, -1);

        Assert.Equal("A1:C1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldAddNextVerticalCellAfterMergedCell()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:B2"));
        sheet.Selection.Select(CellRef.Parse("B1"));
        sheet.Selection.Extend(1, 0);

        Assert.Equal("B1:B3", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldAddNextVerticalCellAfterMergedCellAgain()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:B2"));
        sheet.Selection.Select(CellRef.Parse("B1"));
        sheet.Selection.Extend(1, 0);
        sheet.Selection.Extend(1, 0);

        Assert.Equal("B1:B4", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldHandleMergedCellsConsistentlyWhenMovingLeft()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("D1"));
        sheet.Selection.Extend(0, -1);

        Assert.Equal("B1:D1", sheet.Selection.Range.ToString());
        Assert.Equal("D1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldHandleMergedCellsConsistentlyWhenMovingLeftFromStart()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("B1"));
        sheet.Selection.Extend(0, -1);

        Assert.Equal("A1:C1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldHandleMergedCellsConsistentlyWhenMovingLeftFromEnd()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("C1"));
        sheet.Selection.Extend(0, -1);

        Assert.Equal("A1:C1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldHandleMergedCellsConsistentlyWhenMovingLeftFromEndAndThenRight()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:C1"));
        sheet.Selection.Select(CellRef.Parse("C1"));
        sheet.Selection.Extend(0, -1);
        sheet.Selection.Extend(0, 1);

        Assert.Equal("B1:C1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldHandleMergedCellsConsistentlyWhenMovingLeftFromMiddle()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:D1"));
        sheet.Selection.Select(CellRef.Parse("C1"));
        sheet.Selection.Extend(0, -1);

        Assert.Equal("A1:D1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldAddNextMergedRowAfterMergedRow()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:B2"));
        sheet.MergedCells.Add(RangeRef.Parse("B3:B4"));
        sheet.Selection.Select(CellRef.Parse("B1"));
        sheet.Selection.Extend(1, 0);

        Assert.Equal("B1:B4", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldSubtractNextMergedRowAfterMergedRow()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:B2"));
        sheet.MergedCells.Add(RangeRef.Parse("B3:B4"));
        sheet.Selection.Select(CellRef.Parse("B1"));
        sheet.Selection.Extend(1, 0);
        sheet.Selection.Extend(-1, 0);

        Assert.Equal("B1:B2", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldAddPreviousMergedRowBeforeMergedRow()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:B2"));
        sheet.MergedCells.Add(RangeRef.Parse("B3:B4"));
        sheet.Selection.Select(CellRef.Parse("B3"));
        sheet.Selection.Extend(-1, 0);

        Assert.Equal("B1:B4", sheet.Selection.Range.ToString());
        Assert.Equal("B3", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldSubtractPreviousMergedRowBeforeMergedRow()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B1:B2"));
        sheet.MergedCells.Add(RangeRef.Parse("B3:B4"));
        sheet.Selection.Select(CellRef.Parse("B3"));
        sheet.Selection.Extend(-1, 0);
        sheet.Selection.Extend(1, 0);

        Assert.Equal("B3:B4", sheet.Selection.Range.ToString());
        Assert.Equal("B3", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldAddPreviousVerticalMergedRow()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B2:B3"));
        sheet.Selection.Select(CellRef.Parse("B4"));
        sheet.Selection.Extend(-1, 0);

        Assert.Equal("B2:B4", sheet.Selection.Range.ToString());
        Assert.Equal("B4", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldSubtractPreviousMergedRowFromSelection()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B2:B3"));
        sheet.Selection.Select(CellRef.Parse("B4"));
        sheet.Selection.Extend(-1, 0);

        Assert.Equal("B2:B4", sheet.Selection.Range.ToString());
        sheet.Selection.Extend(1, 0);

        Assert.Equal("B4:B4", sheet.Selection.Range.ToString());
        Assert.Equal("B4", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldSubtractNextMergedRowFromSelection()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B2:B3"));
        sheet.Selection.Select(RangeRef.Parse("B1:B3"));
        sheet.Selection.Extend(-1, 0);

        Assert.Equal("B1:B1", sheet.Selection.Range.ToString());
        Assert.Equal("B1", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldSubtractPreviousVerticalCellBeforeMergedRowFromSelection()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B2:B3"));
        sheet.Selection.Select(RangeRef.Parse("B2:B3"));
        sheet.Selection.Extend(-1, 0);
        Assert.Equal("B1:B3", sheet.Selection.Range.ToString());

        sheet.Selection.Extend(1, 0);

        Assert.Equal("B2:B3", sheet.Selection.Range.ToString());
        Assert.Equal("B2", sheet.Selection.Cell.ToString());
    }

    [Fact]
    public void Extend_ShouldAddPreviousVerticalCellBeforeMergedRow()
    {
        sheet.MergedCells.Add(RangeRef.Parse("B2:B3"));
        sheet.Selection.Select(CellRef.Parse("B2"));
        sheet.Selection.Extend(-1, 0);

        Assert.Equal("B1:B3", sheet.Selection.Range.ToString());
        Assert.Equal("B2", sheet.Selection.Cell.ToString());
    }
}