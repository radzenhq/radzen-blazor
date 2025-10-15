using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class RowsFunctionTests
{
    [Fact]
    public void Rows_Range_ReturnsRowCount()
    {
        var sheet = new Sheet(50, 20);
        sheet.Cells["A1"].Formula = "=ROWS(C1:E4)";
        Assert.Equal(4d, sheet.Cells["A1"].Data.Value);
    }

    [Fact]
    public void Rows_SingleCell_ReturnsOne()
    {
        var sheet = new Sheet(50, 20);
        sheet.Cells["A1"].Formula = "=ROWS(C10)";
        Assert.Equal(1d, sheet.Cells["A1"].Data.Value);
    }

    [Fact]
    public void Rows_SingleRowRange_ReturnsOne()
    {
        var sheet = new Sheet(50, 20);
        sheet.Cells["A1"].Formula = "=ROWS(C10:E10)";
        Assert.Equal(1d, sheet.Cells["A1"].Data.Value);
    }
}