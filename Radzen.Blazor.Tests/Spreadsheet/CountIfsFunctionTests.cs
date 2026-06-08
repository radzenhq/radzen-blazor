using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class CountIfsFunctionTests
{
    readonly Worksheet sheet = new(10, 10);

    [Fact]
    public void ShouldCountWithSingleCriteria()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Value = 10;

        sheet.Cells["B1"].Formula = "=COUNTIFS(A1:A3,\">1\")";

        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldCountWithTwoCriteriaUsingAnd()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Value = 10;
        sheet.Cells["B1"].Value = 100;
        sheet.Cells["B2"].Value = 200;
        sheet.Cells["B3"].Value = 300;

        // A>1 AND B<300 -> only row 2 (5,200)
        sheet.Cells["C1"].Formula = "=COUNTIFS(A1:A3,\">1\",B1:B3,\"<300\")";

        Assert.Equal(1d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldReturnValueErrorForMismatchedRangeSizes()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 2;
        sheet.Cells["B1"].Value = 1;

        sheet.Cells["C1"].Formula = "=COUNTIFS(A1:A2,\">0\",B1:B1,\">0\")";

        Assert.Equal(CellError.Value, sheet.Cells["C1"].Value);
    }
}
