using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class RightFunctionTests
{
    [Fact]
    public void Right_WithCount_ReturnsSuffix()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A2"].Value = "Sale Price";
        sheet.Cells["B1"].Formula = "=RIGHT(A2,5)";
        Assert.Equal("Price", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Right_OmittedCount_DefaultsToOne()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A3"].Value = "Stock Number";
        sheet.Cells["B1"].Formula = "=RIGHT(A3)";
        Assert.Equal("r", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Right_CountExceedsLength_ReturnsWhole()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Value = "Hi";
        sheet.Cells["B1"].Formula = "=RIGHT(A1,5)";
        Assert.Equal("Hi", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Right_NegativeCount_ReturnsValueError()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Value = "Test";
        sheet.Cells["B1"].Formula = "=RIGHT(A1,-1)";
        Assert.Equal(CellError.Value, sheet.Cells["B1"].Data.GetValueOrDefault<CellError>());
    }
}