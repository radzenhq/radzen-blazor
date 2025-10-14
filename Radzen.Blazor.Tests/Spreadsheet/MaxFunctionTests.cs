using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class MaxFunctionTests
{
    readonly Sheet sheet = new(10, 10);

    [Fact]
    public void ShouldReturnLargestValueFromNumbers()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 7;
        sheet.Cells["A3"].Value = 9;
        sheet.Cells["A4"].Value = 27;
        sheet.Cells["A5"].Value = 2;

        sheet.Cells["B1"].Formula = "=MAX(A1:A5)";

        Assert.Equal(27d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldReturnLargestValueFromRangeAndLiteral()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 7;
        sheet.Cells["A3"].Value = 9;
        sheet.Cells["A4"].Value = 27;
        sheet.Cells["A5"].Value = 2;

        sheet.Cells["B1"].Formula = "=MAX(A1:A5,30)";

        Assert.Equal(30d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldIgnoreNonNumericInRange()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = "text";
        sheet.Cells["A3"].Value = true;
        sheet.Cells["A4"].Value = 27;
        sheet.Cells["A5"].Value = null;

        sheet.Cells["B1"].Formula = "=MAX(A1:A5)";

        Assert.Equal(27d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldTreatDirectLogicalAndNumericStringsAsNumbers()
    {
        sheet.Cells["A1"].Formula = "=MAX(\"15\", 5, 10)";
        Assert.Equal(15d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldReturnZeroWhenNoNumbers()
    {
        sheet.Cells["A1"].Value = "a";
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Formula = "=MAX(A1:A2)";

        Assert.Equal(0d, sheet.Cells["A3"].Value);
    }
}


