using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class MonthFunctionTests
{
    [Fact]
    public void Month_FromDateSerial_ReturnsMonth()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=MONTH(VALUE(\"2011-04-15\"))";
        Assert.Equal(4, sheet.Cells["A1"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Month_FromDateValue_ReturnsMonth()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Data = CellData.FromDate(new System.DateTime(2011, 4, 15));
        sheet.Cells["B1"].Formula = "=MONTH(A1)";
        Assert.Equal(4, sheet.Cells["B1"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Month_InvalidText_ReturnsValueError()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=MONTH(\"abc\")";
        Assert.Equal(CellError.Value, sheet.Cells["A1"].Data.GetValueOrDefault<CellError>());
    }
}