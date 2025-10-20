using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class WeekdayFunctionTests
{
    [Fact]
    public void Weekday_Default_SundayToSaturday()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Data = CellData.FromDate(new System.DateTime(2008, 2, 14)); // Thursday
        sheet.Cells["B1"].Formula = "=WEEKDAY(A1)"; // default 1: Sun=1..Sat=7
        Assert.Equal(5, sheet.Cells["B1"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Weekday_Type2_MondayToSunday()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Data = CellData.FromDate(new System.DateTime(2008, 2, 14)); // Thursday
        sheet.Cells["B1"].Formula = "=WEEKDAY(A1, 2)"; // Mon=1..Sun=7
        Assert.Equal(4, sheet.Cells["B1"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Weekday_Type3_MondayZero_SundaySix()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Data = CellData.FromDate(new System.DateTime(2008, 2, 14)); // Thursday
        sheet.Cells["B1"].Formula = "=WEEKDAY(A1, 3)"; // Mon=0..Sun=6
        Assert.Equal(3, sheet.Cells["B1"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Weekday_InvalidReturnType_ReturnsNumError()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Data = CellData.FromDate(new System.DateTime(2008, 2, 14));
        sheet.Cells["B1"].Formula = "=WEEKDAY(A1, 10)"; // invalid
        Assert.Equal(CellError.Num, sheet.Cells["B1"].Data.GetValueOrDefault<CellError>());
    }
}