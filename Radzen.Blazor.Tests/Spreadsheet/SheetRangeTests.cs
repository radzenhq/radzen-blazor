using System;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class SheetRangeTests
{
    [Fact]
    public void Range_Parse_ShouldParseValidRange()
    {
        var range = RangeRef.Parse("A1:B2");
        
        Assert.Equal(new CellRef(0, 0), range.Start);
        Assert.Equal(new CellRef(1, 1), range.End);
    }

    [Fact]
    public void Range_Parse_ShouldThrowOnInvalidRange()
    {
        Assert.Throws<ArgumentException>(() => RangeRef.Parse("A1:B2:C3"));
    }

    [Fact]
    public void Range_GetCells_ShouldReturnAllCellsInRange()
    {
        var range = RangeRef.Parse("A1:B2");
        var cells = range.GetCells().ToList();
        
        Assert.Equal(4, cells.Count);
        Assert.Equal(new CellRef(0, 0), cells[0]); // A1
        Assert.Equal(new CellRef(0, 1), cells[1]); // B1
        Assert.Equal(new CellRef(1, 0), cells[2]); // A2
        Assert.Equal(new CellRef(1, 1), cells[3]); // B2
    }

    [Fact]
    public void Range_ToString_ShouldReturnA1Notation()
    {
        var range = new RangeRef(new CellRef(0, 0), new CellRef(1, 1));
        Assert.Equal("A1:B2", range.ToString());
    }
} 