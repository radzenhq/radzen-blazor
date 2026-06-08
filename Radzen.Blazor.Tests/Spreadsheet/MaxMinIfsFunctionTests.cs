using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class MaxMinIfsFunctionTests
{
    readonly Worksheet sheet = new(10, 10);

    [Fact]
    public void ShouldMaxMatching()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Value = 10;
        sheet.Cells["B1"].Value = 100;
        sheet.Cells["B2"].Value = 200;
        sheet.Cells["B3"].Value = 300;

        sheet.Cells["C1"].Formula = "=MAXIFS(B1:B3,A1:A3,\"<10\")";

        Assert.Equal(200d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldMinMatching()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Value = 10;
        sheet.Cells["B1"].Value = 100;
        sheet.Cells["B2"].Value = 200;
        sheet.Cells["B3"].Value = 300;

        sheet.Cells["C1"].Formula = "=MINIFS(B1:B3,A1:A3,\">1\")";

        Assert.Equal(200d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void ShouldReturnZeroWhenNoMatch()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["B1"].Value = 100;

        sheet.Cells["C1"].Formula = "=MAXIFS(B1:B1,A1:A1,\">100\")";

        Assert.Equal(0d, sheet.Cells["C1"].Value);
    }
}
