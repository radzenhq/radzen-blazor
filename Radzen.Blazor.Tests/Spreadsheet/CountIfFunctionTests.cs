using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class CountIfFunctionTests
{
    readonly Worksheet sheet = new(10, 10);

    [Fact]
    public void ShouldCountNumericGreaterThan()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Value = 10;

        sheet.Cells["B1"].Formula = "=COUNTIF(A1:A3,\">1\")";

        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldCountExactNumber()
    {
        sheet.Cells["A1"].Value = 5;
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Value = 10;

        sheet.Cells["B1"].Formula = "=COUNTIF(A1:A3,5)";

        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldCountTextCaseInsensitively()
    {
        sheet.Cells["A1"].Value = "apple";
        sheet.Cells["A2"].Value = "APPLE";
        sheet.Cells["A3"].Value = "pear";

        sheet.Cells["B1"].Formula = "=COUNTIF(A1:A3,\"apple\")";

        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldCountWithWildcard()
    {
        sheet.Cells["A1"].Value = "apples";
        sheet.Cells["A2"].Value = "oranges";
        sheet.Cells["A3"].Value = "pear";

        sheet.Cells["B1"].Formula = "=COUNTIF(A1:A3,\"*es\")";

        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldCountNotEqualNumber()
    {
        sheet.Cells["A1"].Value = 5;
        sheet.Cells["A2"].Value = 10;
        sheet.Cells["A3"].Value = 5;

        sheet.Cells["B1"].Formula = "=COUNTIF(A1:A3,\"<>5\")";

        Assert.Equal(1d, sheet.Cells["B1"].Value);
    }
}
