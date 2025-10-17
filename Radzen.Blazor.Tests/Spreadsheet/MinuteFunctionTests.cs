using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class MinuteFunctionTests
{
    [Fact]
    public void Minute_FromFraction_ReturnsMinutes()
    {
        var sheet = new Sheet(10, 10);
        // 0.78125 = 18:45 -> minutes 45
        sheet.Cells["A1"].Data = CellData.FromNumber(0.78125);
        sheet.Cells["B1"].Formula = "=MINUTE(A1)";
        Assert.Equal(45, sheet.Cells["B1"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Minute_FromDateTimeValue_ReturnsMinute()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A2"].Data = CellData.FromDate(new System.DateTime(2011, 7, 18, 7, 45, 0));
        sheet.Cells["B2"].Formula = "=MINUTE(A2)";
        Assert.Equal(45, sheet.Cells["B2"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Minute_FromDateOnly_ReturnsZero()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A3"].Data = CellData.FromDate(new System.DateTime(2012, 4, 21));
        sheet.Cells["B3"].Formula = "=MINUTE(A3)";
        Assert.Equal(0, sheet.Cells["B3"].Data.GetValueOrDefault<double>());
    }
}