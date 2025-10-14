using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class ColumnFunctionTests
{
    [Fact]
    public void Column_OmittedReference_ReturnsCurrentColumn()
    {
        var sheet = new Sheet(20, 10);
        sheet.Cells["C10"].Formula = "=COLUMN()";
        Assert.Equal(3d, sheet.Cells["C10"].Data.Value);
    }

    [Fact]
    public void Column_SingleCellReference_ReturnsThatColumn()
    {
        var sheet = new Sheet(20, 10);
        sheet.Cells["A1"].Formula = "=COLUMN(C10)";
        Assert.Equal(3d, sheet.Cells["A1"].Data.Value);
    }

    [Fact]
    public void Column_RangeReference_SingleRow_ReturnsLeftmostColumn()
    {
        var sheet = new Sheet(20, 10);
        sheet.Cells["B2"].Formula = "=COLUMN(C10:E10)";
        Assert.Equal(3d, sheet.Cells["B2"].Data.Value);
    }

    [Fact]
    public void Column_RangeReference_MultiRowAndColumn_IsError()
    {
        var sheet = new Sheet(20, 10);
        sheet.Cells["B2"].Formula = "=COLUMN(C10:D20)";
        Assert.Equal(CellError.Value, sheet.Cells["B2"].Data.Value);
    }
}


