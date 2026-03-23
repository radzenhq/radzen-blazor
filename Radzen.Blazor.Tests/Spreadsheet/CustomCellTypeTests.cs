using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class CustomCellTypeTests
{
    readonly Worksheet sheet = new(10, 10);

    [Fact]
    public void SetCustomType_SingleCell_GetCustomType_ReturnsType()
    {
        sheet.Cells.SetCustomType(new CellRef(1, 2), "progress");

        Assert.Equal("progress", sheet.Cells.GetCustomType(1, 2));
    }

    [Fact]
    public void SetCustomType_Range_GetCustomType_ReturnsTypeForCellsInRange()
    {
        var range = new RangeRef(new CellRef(0, 1), new CellRef(4, 1));
        sheet.Cells.SetCustomType(range, "rating");

        Assert.Equal("rating", sheet.Cells.GetCustomType(0, 1));
        Assert.Equal("rating", sheet.Cells.GetCustomType(2, 1));
        Assert.Equal("rating", sheet.Cells.GetCustomType(4, 1));
    }

    [Fact]
    public void SetCustomType_Range_GetCustomType_ReturnsNullForCellsOutsideRange()
    {
        var range = new RangeRef(new CellRef(0, 1), new CellRef(4, 1));
        sheet.Cells.SetCustomType(range, "rating");

        Assert.Null(sheet.Cells.GetCustomType(0, 0));
        Assert.Null(sheet.Cells.GetCustomType(5, 1));
        Assert.Null(sheet.Cells.GetCustomType(0, 2));
    }

    [Fact]
    public void SetCustomType_Null_RemovesType()
    {
        var range = new RangeRef(new CellRef(0, 0), new CellRef(2, 2));
        sheet.Cells.SetCustomType(range, "progress");

        Assert.Equal("progress", sheet.Cells.GetCustomType(1, 1));

        sheet.Cells.SetCustomType(range, null);

        Assert.Null(sheet.Cells.GetCustomType(1, 1));
    }

    [Fact]
    public void GetCustomType_NoTypeSet_ReturnsNull()
    {
        Assert.Null(sheet.Cells.GetCustomType(0, 0));
        Assert.Null(sheet.Cells.GetCustomType(5, 5));
    }

    [Fact]
    public void SetCustomType_OverlappingRanges_FirstMatchWins()
    {
        var range1 = new RangeRef(new CellRef(0, 0), new CellRef(5, 5));
        var range2 = new RangeRef(new CellRef(2, 2), new CellRef(3, 3));

        sheet.Cells.SetCustomType(range1, "first");
        sheet.Cells.SetCustomType(range2, "second");

        // Cell (0,0) only in range1
        Assert.Equal("first", sheet.Cells.GetCustomType(0, 0));

        // Cell (6,6) outside both ranges
        Assert.Null(sheet.Cells.GetCustomType(6, 6));
    }

    [Fact]
    public void SetCustomType_CellRef_CanBeOverwritten()
    {
        sheet.Cells.SetCustomType(new CellRef(0, 0), "progress");
        Assert.Equal("progress", sheet.Cells.GetCustomType(0, 0));

        sheet.Cells.SetCustomType(new CellRef(0, 0).ToRange(), "rating");
        Assert.Equal("rating", sheet.Cells.GetCustomType(0, 0));
    }

    [Fact]
    public void SetCustomType_MultipleRanges_IndependentLookup()
    {
        var rangeA = new RangeRef(new CellRef(0, 0), new CellRef(0, 9));
        var rangeB = new RangeRef(new CellRef(1, 0), new CellRef(1, 9));

        sheet.Cells.SetCustomType(rangeA, "header");
        sheet.Cells.SetCustomType(rangeB, "data");

        Assert.Equal("header", sheet.Cells.GetCustomType(0, 5));
        Assert.Equal("data", sheet.Cells.GetCustomType(1, 5));
    }
}
