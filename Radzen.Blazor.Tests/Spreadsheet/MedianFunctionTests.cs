using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class MedianFunctionTests
{
    readonly Worksheet sheet = new(10, 10);

    [Fact]
    public void ShouldReturnMiddleOfOddCount()
    {
        sheet.Cells["A1"].Formula = "=MEDIAN(1,2,3)";
        Assert.Equal(2d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldAverageMiddleTwoOfEvenCount()
    {
        sheet.Cells["A1"].Formula = "=MEDIAN(1,2,3,4)";
        Assert.Equal(2.5, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldIgnoreTextInRange()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = "x";
        sheet.Cells["A3"].Value = 3;

        sheet.Cells["B1"].Formula = "=MEDIAN(A1:A3)";

        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }
}
