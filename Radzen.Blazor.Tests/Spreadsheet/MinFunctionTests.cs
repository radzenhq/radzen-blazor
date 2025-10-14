using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class MinFunctionTests
{
    readonly Sheet sheet = new(10, 10);

    [Fact]
    public void ShouldReturnSmallestValueFromNumbers()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 7;
        sheet.Cells["A3"].Value = 9;
        sheet.Cells["A4"].Value = 27;
        sheet.Cells["A5"].Value = 2;

        sheet.Cells["B1"].Formula = "=MIN(A1:A5)";

        Assert.Equal(2d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldReturnSmallestValueFromRangeAndLiteral()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 7;
        sheet.Cells["A3"].Value = 9;
        sheet.Cells["A4"].Value = 27;
        sheet.Cells["A5"].Value = 2;

        sheet.Cells["B1"].Formula = "=MIN(A1:A5,1)";

        Assert.Equal(1d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldIgnoreNonNumericInRange()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = "text";
        sheet.Cells["A3"].Value = true;
        sheet.Cells["A4"].Value = 27;
        sheet.Cells["A5"].Value = null;

        sheet.Cells["B1"].Formula = "=MIN(A1:A5)";

        Assert.Equal(10d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldTreatDirectNumericStringsAsNumbers()
    {
        sheet.Cells["A1"].Formula = "=MIN(\"15\", 5, 10)";
        Assert.Equal(5d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldReturnZeroWhenNoNumbers()
    {
        sheet.Cells["A1"].Value = "a";
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Formula = "=MIN(A1:A2)";

        Assert.Equal(0d, sheet.Cells["A3"].Value);
    }
}


