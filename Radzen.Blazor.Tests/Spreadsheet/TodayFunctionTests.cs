using Xunit;
using Radzen.Blazor.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class TodayFunctionTests
{
    [Fact]
    public void Today_ReturnsCurrentDate()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=TODAY()";
        var dt = sheet.Cells["A1"].Data.GetValueOrDefault<System.DateTime>();
        Assert.Equal(System.DateTime.Today.Date, dt.Date);
    }

    [Fact]
    public void Today_PlusFiveDays()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=TODAY()+5";
        var serial = sheet.Cells["A1"].Data.GetValueOrDefault<double>();
        var expected = System.DateTime.Today.AddDays(5).ToNumber();
        Assert.Equal(expected, serial);
    }
}