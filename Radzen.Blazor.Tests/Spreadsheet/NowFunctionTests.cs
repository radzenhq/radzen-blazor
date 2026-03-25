using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class NowFunctionTests
{
    [Fact]
    public void Now_ReturnsCurrentDateTime()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells["A1"].Formula = "=NOW()";
        var dt = sheet.Cells["A1"].Data.GetValueOrDefault<System.DateTime>();
        Assert.Equal(System.DateTime.Today, dt.Date);
    }

    [Fact]
    public void Now_MinusToday_IsFractionalDayBetween0And1()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells["A1"].Formula = "=NOW()-TODAY()";
        var serial = sheet.Cells["A1"].Data.GetValueOrDefault<double>();
        Assert.True(serial >= 0 && serial < 1);
    }

    [Fact]
    public void Now_PlusSevenDays_MinusToday_IsBetweenSevenAndEight()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells["A1"].Formula = "=NOW()+7 - TODAY()";
        var serial = sheet.Cells["A1"].Data.GetValueOrDefault<double>();
        Assert.True(serial >= 7 && serial < 8);
    }
}