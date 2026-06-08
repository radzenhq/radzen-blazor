using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class ProductFunctionTests
{
    readonly Worksheet sheet = new(5, 5);

    [Fact]
    public void ShouldMultiplyValues()
    {
        sheet.Cells["A1"].Formula = "=PRODUCT(2,3,4)";
        Assert.Equal(24d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldIgnoreTextInRange()
    {
        sheet.Cells["A1"].Value = 2;
        sheet.Cells["A2"].Value = "x";
        sheet.Cells["A3"].Value = 5;
        sheet.Cells["B1"].Formula = "=PRODUCT(A1:A3)";
        Assert.Equal(10d, sheet.Cells["B1"].Value);
    }
}
