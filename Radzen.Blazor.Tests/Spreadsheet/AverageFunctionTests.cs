using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class AverageFunctionTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void ShouldEvaluateAverageFunctionWithTwoArguments()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 15;
        sheet.Cells["A3"].Formula = "=AVERAGE(A1,A2)";

        Assert.Equal(12.5, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateAverageFunctionWithEmptyCells()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A3"].Formula = "=AVERAGE(A1,A2)";

        Assert.Equal(10.0, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateAverageFunctionWithMultipleArguments()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 15;
        sheet.Cells["A3"].Value = 20;
        sheet.Cells["A4"].Formula = "=AVERAGE(A1,A2,A3)";

        Assert.Equal(15.0, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldReturnDiv0ErrorForEmptyAverageFunction()
    {
        sheet.Cells["A1"].Formula = "=AVERAGE()";

        Assert.Equal(CellError.Div0, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldReturnDiv0ErrorForAverageFunctionWithNoNumericValues()
    {
        sheet.Cells["A1"].Value = "text";
        sheet.Cells["A2"].Value = "";
        sheet.Cells["A3"].Formula = "=AVERAGE(A1,A2)";

        Assert.Equal(CellError.Div0, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateAverageFunctionWithRange()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 15;
        sheet.Cells["A3"].Formula = "=AVERAGE(A1:A2)";

        Assert.Equal(12.5, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateAverageFunctionWithMixedTypes()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 15.5;
        sheet.Cells["A3"].Formula = "=AVERAGE(A1,A2)";

        Assert.Equal(12.75, sheet.Cells["A3"].Value);

        sheet.Cells["A4"].Value = 2.5;
        sheet.Cells["A5"].Formula = "=AVERAGE(A4,A1)";

        Assert.Equal(6.25, sheet.Cells["A5"].Value);
    }

    [Fact]
    public void ShouldEvaluateAverageFunctionIgnoringTextAndLogicalValues()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = "text";
        sheet.Cells["A3"].Value = true;
        sheet.Cells["A4"].Value = 20;
        sheet.Cells["A5"].Formula = "=AVERAGE(A1,A2,A3,A4)";

        Assert.Equal(15.0, sheet.Cells["A5"].Value);
    }

    [Fact]
    public void ShouldEvaluateAverageFunctionIncludingZeroValues()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 0;
        sheet.Cells["A3"].Value = 20;
        sheet.Cells["A4"].Formula = "=AVERAGE(A1,A2,A3)";

        Assert.Equal(10.0, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldCreateRefErrorWhenAverageRangeOutOfBounds()
    {
        sheet.Cells["A1"].Formula = "=AVERAGE(A2:A6)";

        Assert.Equal(CellError.Ref, sheet.Cells["A1"].Value);
    }
}


