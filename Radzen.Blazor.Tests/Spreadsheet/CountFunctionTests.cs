using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class CountFunctionTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void ShouldEvaluateCountFunctionWithTwoArguments()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 15;
        sheet.Cells["A3"].Formula = "=COUNT(A1,A2)";

        Assert.Equal(2.0, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateCountFunctionWithEmptyCells()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A3"].Formula = "=COUNT(A1,A2)";

        Assert.Equal(1.0, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateCountFunctionWithMultipleArguments()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 15;
        sheet.Cells["A3"].Value = 20;
        sheet.Cells["A4"].Formula = "=COUNT(A1,A2,A3)";

        Assert.Equal(3.0, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldReturnZeroForEmptyCountFunction()
    {
        sheet.Cells["A1"].Formula = "=COUNT()";

        Assert.Equal(0.0, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldEvaluateCountFunctionWithRange()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 15;
        sheet.Cells["A3"].Formula = "=COUNT(A1:A2)";

        Assert.Equal(2.0, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateCountFunctionWithMixedTypes()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 15.5;
        sheet.Cells["A3"].Formula = "=COUNT(A1,A2)";

        Assert.Equal(2.0, sheet.Cells["A3"].Value);

        sheet.Cells["A4"].Value = 2.5;
        sheet.Cells["A5"].Formula = "=COUNT(A4,A1)";

        Assert.Equal(2.0, sheet.Cells["A5"].Value);
    }

    [Fact]
    public void ShouldEvaluateCountFunctionIncludingLogicalValues()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = true;
        sheet.Cells["A3"].Value = false;
        sheet.Cells["A4"].Value = 20;
        sheet.Cells["A5"].Formula = "=COUNT(A1,A2,A3,A4)";

        Assert.Equal(4.0, sheet.Cells["A5"].Value);
    }

    [Fact]
    public void ShouldEvaluateCountFunctionIncludingTextRepresentationsOfNumbers()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = "15";
        sheet.Cells["A3"].Value = "text";
        sheet.Cells["A4"].Value = "3.14";
        sheet.Cells["A5"].Formula = "=COUNT(A1,A2,A3,A4)";

        Assert.Equal(3.0, sheet.Cells["A5"].Value);
    }

    [Fact]
    public void ShouldEvaluateCountFunctionIgnoringTextAndEmptyCells()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = "text";
        sheet.Cells["A3"].Value = "";
        sheet.Cells["A4"].Value = 20;
        sheet.Cells["A5"].Formula = "=COUNT(A1,A2,A3,A4)";

        Assert.Equal(2.0, sheet.Cells["A5"].Value);
    }

    [Fact]
    public void ShouldEvaluateCountFunctionIncludingZeroValues()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 0;
        sheet.Cells["A3"].Value = 20;
        sheet.Cells["A4"].Formula = "=COUNT(A1,A2,A3)";

        Assert.Equal(3.0, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldEvaluateCountFunctionWithAllNumericValues()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 20;
        sheet.Cells["A3"].Value = 30;
        sheet.Cells["A4"].Formula = "=COUNT(A1,A2,A3)";

        Assert.Equal(3.0, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldCreateRefErrorWhenCountRangeOutOfBounds()
    {
        sheet.Cells["A1"].Formula = "=COUNT(A2:A6)";

        Assert.Equal(CellError.Ref, sheet.Cells["A1"].Value);
    }
}


