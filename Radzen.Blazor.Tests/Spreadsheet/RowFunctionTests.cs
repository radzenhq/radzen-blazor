using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class RowFunctionTests
{
    [Fact]
    public void Row_OmittedReference_ReturnsCurrentRow()
    {
        var sheet = new Sheet(20, 10);
        sheet.Cells["C10"].Formula = "=ROW()";
        Assert.Equal(10d, sheet.Cells["C10"].Data.Value);
    }

    [Fact]
    public void Row_SingleCellReference_ReturnsThatRow()
    {
        var sheet = new Sheet(20, 10);
        sheet.Cells["A1"].Formula = "=ROW(C10)";
        Assert.Equal(10d, sheet.Cells["A1"].Data.Value);
    }

    [Fact]
    public void Row_RangeReference_ReturnsTopLeftRow()
    {
        var sheet = new Sheet(20, 10);
        sheet.Cells["B2"].Formula = "=ROW(C10:E10)";
        Assert.Equal(10d, sheet.Cells["B2"].Data.Value);
    }

    [Fact]
    public void Row_RangeReference_MultiRowAndColumn_IsError()
    {
        var sheet = new Sheet(20, 10);
        sheet.Cells["B2"].Formula = "=ROW(C10:D20)";
        Assert.Equal(CellError.Value, sheet.Cells["B2"].Data.Value);
    }
}