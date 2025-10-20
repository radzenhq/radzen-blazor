using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class WeeknumFunctionTests
{
    [Fact]
    public void Weeknum_Default_SundayStart()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Data = CellData.FromDate(new System.DateTime(2012, 3, 9)); // Excel example
        sheet.Cells["B1"].Formula = "=WEEKNUM(A1)"; // default 1
        Assert.Equal(10, sheet.Cells["B1"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Weeknum_Type2_MondayStart()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Data = CellData.FromDate(new System.DateTime(2012, 3, 9));
        sheet.Cells["B1"].Formula = "=WEEKNUM(A1, 2)"; // Monday start, System 1
        Assert.Equal(11, sheet.Cells["B1"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Weeknum_Type21_ISO()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Data = CellData.FromDate(new System.DateTime(2012, 3, 9));
        sheet.Cells["B1"].Formula = "=WEEKNUM(A1, 21)"; // ISO
        Assert.Equal(10, sheet.Cells["B1"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Weeknum_InvalidReturnType_ReturnsNumError()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Data = CellData.FromDate(new System.DateTime(2012, 3, 9));
        sheet.Cells["B1"].Formula = "=WEEKNUM(A1, 4)"; // invalid code
        Assert.Equal(CellError.Num, sheet.Cells["B1"].Data.GetValueOrDefault<CellError>());
    }
}