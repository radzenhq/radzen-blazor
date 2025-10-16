using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class LeftFunctionTests
{
    [Fact]
    public void Left_WithCount_ReturnsPrefix()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A2"].Value = "Sale Price";
        sheet.Cells["B1"].Formula = "=LEFT(A2,4)";
        Assert.Equal("Sale", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Left_OmittedCount_DefaultsToOne()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A3"].Value = "Sweden";
        sheet.Cells["B1"].Formula = "=LEFT(A3)";
        Assert.Equal("S", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Left_CountExceedsLength_ReturnsWhole()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Value = "Hi";
        sheet.Cells["B1"].Formula = "=LEFT(A1,5)";
        Assert.Equal("Hi", sheet.Cells["B1"].Data.Value);
    }

    [Fact]
    public void Left_NegativeCount_ReturnsValueError()
    {
        var sheet = new Sheet(10, 10);
        sheet.Cells["A1"].Value = "Test";
        sheet.Cells["B1"].Formula = "=LEFT(A1,-1)";
        Assert.Equal(CellError.Value, sheet.Cells["B1"].Data.GetValueOrDefault<CellError>());
    }
}