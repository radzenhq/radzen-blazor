using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class ColumnsFunctionTests
{
    [Fact]
    public void Columns_Range_ReturnsColumnCount()
    {
        var sheet = new Sheet(50, 20);
        sheet.Cells["A1"].Formula = "=COLUMNS(C1:E4)";
        Assert.Equal(3d, sheet.Cells["A1"].Data.Value);
    }

    [Fact]
    public void Columns_SingleCell_ReturnsOne()
    {
        var sheet = new Sheet(50, 20);
        sheet.Cells["A1"].Formula = "=COLUMNS(C10)";
        Assert.Equal(1d, sheet.Cells["A1"].Data.Value);
    }

    [Fact]
    public void Columns_SingleColumnRange_ReturnsOne()
    {
        var sheet = new Sheet(50, 20);
        sheet.Cells["A1"].Formula = "=COLUMNS(C10:C20)";
        Assert.Equal(1d, sheet.Cells["A1"].Data.Value);
    }
}