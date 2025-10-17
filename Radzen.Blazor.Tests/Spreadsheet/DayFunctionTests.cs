using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class DayFunctionTests
{
    [Fact]
    public void Day_FromDateSerial_ReturnsDay()
    {
        var sheet = new Sheet(10, 10);
        // Using DATEVALUE via VALUE on a date string to get a serial
        sheet.Cells["A1"].Formula = "=DAY(VALUE(\"2011-04-15\"))";
        Assert.Equal(15, sheet.Cells["A1"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Day_FromDateValue_ReturnsDay()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Data = CellData.FromDate(new System.DateTime(2011, 4, 15));
        sheet.Cells["B1"].Formula = "=DAY(A1)";
        Assert.Equal(15, sheet.Cells["B1"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Day_InvalidText_ReturnsValueError()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=DAY(\"abc\")";
        Assert.Equal(CellError.Value, sheet.Cells["A1"].Data.GetValueOrDefault<CellError>());
    }
}