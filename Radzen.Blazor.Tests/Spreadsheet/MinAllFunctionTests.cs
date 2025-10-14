using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class MinAllFunctionTests
{
    readonly Sheet sheet = new(10, 10);

    [Fact]
    public void ShouldEvaluateLogicalValuesInRange()
    {
        sheet.Cells["A1"].Value = true;   // 1
        sheet.Cells["A2"].Value = false;  // 0
        sheet.Cells["A3"].Value = 5;      // 5

        sheet.Cells["B1"].Formula = "=MINA(A1:A3)";

        Assert.Equal(0d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldTreatTextInRangeAsZeroAndNumericTextAsNumber()
    {
        sheet.Cells["A1"].Value = "abc"; // -> 0
        sheet.Cells["A2"].Value = "15";  // -> 15
        sheet.Cells["A3"].Value = 10;

        sheet.Cells["B1"].Formula = "=MINA(A1:A3)";

        Assert.Equal(0d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldCountDirectLogicalAndTextArguments()
    {
        sheet.Cells["A1"].Formula = "=MINA(1=1, \"7\", 1=2)"; // TRUE, "7", FALSE -> min is 0
        Assert.Equal(0d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldReturnZeroWhenNoValues()
    {
        sheet.Cells["A1"].Value = null; // empty
        sheet.Cells["A2"].Value = "";  // empty string -> Empty

        sheet.Cells["B1"].Formula = "=MINA(A1:A2)";

        Assert.Equal(0d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldPropagateErrors()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 0;
        sheet.Cells["A3"].Formula = "=A1/A2"; // #DIV/0!

        sheet.Cells["B1"].Formula = "=MINA(A1:A3)";

        Assert.Equal(CellError.Div0, sheet.Cells["B1"].Value);
    }
}


