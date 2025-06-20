using System;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class CellStoreTests
{
    readonly CellStore cellStore = new(new Sheet(5, 5));

    [Fact]
    public void CellStore_ShouldReturnNewCell_WhenCellDoesNotExist()
    {
        var cell = cellStore[0, 0];

        Assert.NotNull(cell);
    }

    [Fact]
    public void CellStore_ShouldThrowArgumentOutOfRangeException_WhenRowExceedsMax()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => cellStore[5, 0]);
    }

    [Fact]
    public void CellStore_ShouldThrowArgumentOutOfRangeException_WhenColumnExceedsMax()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => cellStore[0, 5]);
    }

    [Fact]
    public void CellStore_ShouldReturnExistingCell_WhenCellExists()
    {
        var expectedCell = new Cell(cellStore.Sheet, new CellRef(0, 0));
        cellStore[0, 0] = expectedCell;
        var cell = cellStore[0, 0];
        Assert.Same(expectedCell, cell);
    }

    [Fact]
    public void CellStore_ShouldReturnExistingCell_ViaA1Notation()
    {
        var expectedCell = new Cell(cellStore.Sheet, new CellRef(0, 0));

        cellStore[0, 0] = expectedCell;

        var cell = cellStore["A1"];

        Assert.Same(expectedCell, cell);
    }

    [Fact]
    public void CellStore_ShouldThrowException_WhenInvalidA1Notation()
    {
        Assert.Throws<ArgumentException>(() => cellStore["Invalid"]);
    }

    [Fact]
    public void CellStore_ShouldSupport_MultipleLettersInA1Notation()
    {
        var cellStore = new CellStore(new Sheet(5, 30));
        var expectedCell = new Cell(cellStore.Sheet, new CellRef(0, 26));

        cellStore[0, 26] = expectedCell;

        var cell = cellStore["AA1"];

        Assert.Same(expectedCell, cell);
    }
}