using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class SecondFunctionTests
{
    [Fact]
    public void Second_FromDateTimeWithSeconds_ReturnsSeconds()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Data = CellData.FromDate(new System.DateTime(2020, 1, 1, 16, 48, 18));
        sheet.Cells["B1"].Formula = "=SECOND(A1)";
        Assert.Equal(18, sheet.Cells["B1"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Second_FromDateTimeWithoutSeconds_ReturnsZero()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A2"].Data = CellData.FromDate(new System.DateTime(2020, 1, 1, 16, 48, 0));
        sheet.Cells["B2"].Formula = "=SECOND(A2)";
        Assert.Equal(0, sheet.Cells["B2"].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Second_FromDateOnly_ReturnsZero()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A3"].Data = CellData.FromDate(new System.DateTime(2020, 1, 1));
        sheet.Cells["B3"].Formula = "=SECOND(A3)";
        Assert.Equal(0, sheet.Cells["B3"].Data.GetValueOrDefault<double>());
    }
}