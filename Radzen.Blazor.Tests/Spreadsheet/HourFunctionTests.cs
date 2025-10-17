using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class HourFunctionTests
{
    [Fact]
    public void Hour_FromFraction_Returns18()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A2"].Data = CellData.FromNumber(0.75); // 18:00
        sheet.Cells["B2"].Formula = "=HOUR(A2)";
        Assert.Equal(18, sheet.Cells["B2"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Hour_FromDateTimeValue_ReturnsHour()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A3"].Data = CellData.FromDate(new System.DateTime(2011, 7, 18, 7, 45, 0));
        sheet.Cells["B3"].Formula = "=HOUR(A3)";
        Assert.Equal(7, sheet.Cells["B3"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Hour_FromDateOnly_ReturnsZero()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A4"].Data = CellData.FromDate(new System.DateTime(2012, 4, 21));
        sheet.Cells["B4"].Formula = "=HOUR(A4)";
        Assert.Equal(0, sheet.Cells["B4"].Data.GetValueOrDefault<double>());
    }
}