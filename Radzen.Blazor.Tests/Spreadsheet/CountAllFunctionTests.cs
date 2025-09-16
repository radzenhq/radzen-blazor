using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class CountAllFunctionTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void ShouldEvaluateCountaFunctionWithTwoArguments()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 15;
        sheet.Cells["A3"].Formula = "=COUNTA(A1,A2)";

        Assert.Equal(2.0, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateCountaFunctionWithEmptyCells()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A3"].Formula = "=COUNTA(A1,A2)";

        Assert.Equal(1.0, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateCountaFunctionWithMultipleArguments()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 15;
        sheet.Cells["A3"].Value = 20;
        sheet.Cells["A4"].Formula = "=COUNTA(A1,A2,A3)";

        Assert.Equal(3.0, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldReturnZeroForEmptyCountaFunction()
    {
        sheet.Cells["A1"].Formula = "=COUNTA()";

        Assert.Equal(0.0, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldEvaluateCountaFunctionWithRange()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 15;
        sheet.Cells["A3"].Formula = "=COUNTA(A1:A2)";

        Assert.Equal(2.0, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateCountaFunctionIncludingTextValues()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = "text";
        sheet.Cells["A3"].Value = 20;
        sheet.Cells["A4"].Formula = "=COUNTA(A1,A2,A3)";

        Assert.Equal(3.0, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldEvaluateCountaFunctionIncludingLogicalValues()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = true;
        sheet.Cells["A3"].Value = false;
        sheet.Cells["A4"].Value = 20;
        sheet.Cells["A5"].Formula = "=COUNTA(A1,A2,A3,A4)";

        Assert.Equal(4.0, sheet.Cells["A5"].Value);
    }

    [Fact]
    public void ShouldEvaluateCountaFunctionIncludingEmptyStrings()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = "";
        sheet.Cells["A3"].Value = 20;
        sheet.Cells["A4"].Formula = "=COUNTA(A1,A2,A3)";

        Assert.Equal(3.0, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldEvaluateCountaFunctionIgnoringTrulyEmptyCells()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = "text";
        sheet.Cells["A4"].Value = 20;
        sheet.Cells["A5"].Formula = "=COUNTA(A1,A2,A3,A4)";

        Assert.Equal(3.0, sheet.Cells["A5"].Value);
    }

    [Fact]
    public void ShouldEvaluateCountaFunctionWithAllNumericValues()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 20;
        sheet.Cells["A3"].Value = 30;
        sheet.Cells["A4"].Formula = "=COUNTA(A1,A2,A3)";

        Assert.Equal(3.0, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldShowDifferenceBetweenCountAndCounta()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = "text";
        sheet.Cells["A3"].Value = true;
        sheet.Cells["A4"].Value = "";
        sheet.Cells["A5"].Value = 20;
        sheet.Cells["B1"].Formula = "=COUNT(A1,A2,A3,A4,A5)";
        sheet.Cells["B2"].Formula = "=COUNTA(A1,A2,A3,A4,A5)";

        Assert.Equal(3.0, sheet.Cells["B1"].Value);
        Assert.Equal(5.0, sheet.Cells["B2"].Value);
    }

    [Fact]
    public void ShouldEvaluateCountaFunctionWithMixedTypes()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = "text";
        sheet.Cells["A3"].Value = true;
        sheet.Cells["A4"].Value = "";
        sheet.Cells["A5"].Value = 3.14;
        sheet.Cells["B1"].Formula = "=COUNTA(A1,A2,A3,A4,A5)";

        Assert.Equal(5.0, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldCreateRefErrorWhenCountaRangeOutOfBounds()
    {
        sheet.Cells["A1"].Formula = "=COUNTA(A2:A6)";

        Assert.Equal(CellError.Ref, sheet.Cells["A1"].Value);
    }
}


