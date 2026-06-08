using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class SumIfsFunctionTests
{
    readonly Worksheet sheet = new(10, 10);

    [Fact]
    public void ShouldSumWithSingleCriteria()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Value = 10;
        sheet.Cells["B1"].Value = 100;
        sheet.Cells["B2"].Value = 200;
        sheet.Cells["B3"].Value = 300;

        sheet.Cells["C1"].Formula = "=SUMIFS(B1:B3,A1:A3,\">1\")";

        Assert.Equal(500d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldSumWithTwoCriteria()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Value = 10;
        sheet.Cells["B1"].Value = 100;
        sheet.Cells["B2"].Value = 200;
        sheet.Cells["B3"].Value = 300;

        // A>1 AND A<10 -> only row 2 (B=200)
        sheet.Cells["C1"].Formula = "=SUMIFS(B1:B3,A1:A3,\">1\",A1:A3,\"<10\")";

        Assert.Equal(200d, sheet.Cells["C1"].Value);
    }
}
