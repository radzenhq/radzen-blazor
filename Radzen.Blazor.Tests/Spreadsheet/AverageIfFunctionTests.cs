using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class AverageIfFunctionTests
{
    readonly Worksheet sheet = new(10, 10);

    [Fact]
    public void ShouldAverageMatching()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Value = 10;

        sheet.Cells["B1"].Formula = "=AVERAGEIF(A1:A3,\">1\")";

        Assert.Equal(7.5, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldAverageWithAverageRange()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Value = 10;
        sheet.Cells["B1"].Value = 100;
        sheet.Cells["B2"].Value = 200;
        sheet.Cells["B3"].Value = 300;

        sheet.Cells["C1"].Formula = "=AVERAGEIF(A1:A3,\">1\",B1:B3)";

        Assert.Equal(250d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldReturnDiv0WhenNoMatch()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 2;

        sheet.Cells["B1"].Formula = "=AVERAGEIF(A1:A2,\">100\")";

        Assert.Equal(CellError.Div0, sheet.Cells["B1"].Value);
    }
}
