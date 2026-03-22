using System;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class SortBoundsTests
{
    [Fact]
    public void Sort_ShouldThrow_WhenKeyIndexBelowRange()
    {
        var sheet = new Worksheet(5, 5);
        sheet.Cells["B1"].Value = 3;
        sheet.Cells["B2"].Value = 1;
        sheet.Cells["C1"].Value = "c";
        sheet.Cells["C2"].Value = "a";

        var range = new RangeRef(CellRef.Parse("B1"), CellRef.Parse("C2"));

        // keyIndex 0 (column A) is outside range B:C
        Assert.Throws<ArgumentOutOfRangeException>(() => sheet.Sort(range, SortOrder.Ascending, 0));
    }

    [Fact]
    public void Sort_ShouldThrow_WhenKeyIndexAboveRange()
    {
        var sheet = new Worksheet(5, 5);
        sheet.Cells["B1"].Value = 3;
        sheet.Cells["B2"].Value = 1;
        sheet.Cells["C1"].Value = "c";
        sheet.Cells["C2"].Value = "a";

        var range = new RangeRef(CellRef.Parse("B1"), CellRef.Parse("C2"));

        // keyIndex 3 (column D) is outside range B:C
        Assert.Throws<ArgumentOutOfRangeException>(() => sheet.Sort(range, SortOrder.Ascending, 3));
    }

    [Fact]
    public void Sort_ShouldWork_WhenKeyIndexInsideRange()
    {
        var sheet = new Worksheet(5, 5);
        sheet.Cells["B1"].Value = 3;
        sheet.Cells["B2"].Value = 1;
        sheet.Cells["C1"].Value = "c";
        sheet.Cells["C2"].Value = "a";

        var range = new RangeRef(CellRef.Parse("B1"), CellRef.Parse("C2"));

        // keyIndex 1 (column B) is inside range B:C
        sheet.Sort(range, SortOrder.Ascending, 1);

        Assert.Equal(1d, sheet.Cells["B1"].Value);
        Assert.Equal(3d, sheet.Cells["B2"].Value);
        Assert.Equal("a", sheet.Cells["C1"].Value);
        Assert.Equal("c", sheet.Cells["C2"].Value);
    }
}
