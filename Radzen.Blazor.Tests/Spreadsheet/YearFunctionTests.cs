using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class YearFunctionTests
{
    [Fact]
    public void Year_FromDateSerial_ReturnsYear()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=YEAR(VALUE(\"2025-05-23\"))";
        Assert.Equal(2025, sheet.Cells["A1"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Year_FromDateValue_ReturnsYear()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Data = CellData.FromDate(new System.DateTime(2023, 7, 5));
        sheet.Cells["B1"].Formula = "=YEAR(A1)";
        Assert.Equal(2023, sheet.Cells["B1"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Year_InvalidText_ReturnsValueError()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Formula = "=YEAR(\"abc\")";
        Assert.Equal(CellError.Value, sheet.Cells["A1"].Data.GetValueOrDefault<CellError>());
    }
}