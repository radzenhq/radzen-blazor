using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class AndFunctionTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void ShouldEvaluateAndFunctionWithAllTrueValues()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Value = true;
        sheet.Cells["A3"].Formula = "=AND(A1,A2)";

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateAndFunctionWithOneFalseValue()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Formula = "=AND(A1,A2)";

        Assert.Equal(false, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateAndFunctionWithAllFalseValues()
    {
        sheet.Cells["A1"].Value = false;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Formula = "=AND(A1,A2)";

        Assert.Equal(false, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateAndFunctionWithNumericValues()
    {
        sheet.Cells["A1"].Value = 5;
        sheet.Cells["A2"].Value = 10;
        sheet.Cells["A3"].Formula = "=AND(A1>1,A2<100)";

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateAndFunctionWithZeroAsFalse()
    {
        sheet.Cells["A1"].Value = 0;
        sheet.Cells["A2"].Value = 1;
        sheet.Cells["A3"].Formula = "=AND(A1,A2)";

        Assert.Equal(false, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateAndFunctionWithNonZeroAsTrue()
    {
        sheet.Cells["A1"].Value = 5;
        sheet.Cells["A2"].Value = 10;
        sheet.Cells["A3"].Formula = "=AND(A1,A2)";

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateAndFunctionWithStringValues()
    {
        sheet.Cells["A1"].Value = "test";
        sheet.Cells["A2"].Value = "hello";
        sheet.Cells["A3"].Formula = "=AND(A1,A2)";

        Assert.Equal(CellError.Value, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateAndFunctionWithEmptyStringAsFalse()
    {
        sheet.Cells["A2"].Value = "hello";
        sheet.Cells["A3"].Formula = "=AND(A1,A2)";

        Assert.Equal(CellError.Value, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateAndFunctionWithMultipleArguments()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Value = true;
        sheet.Cells["A3"].Value = true;
        sheet.Cells["A4"].Formula = "=AND(A1,A2,A3)";

        Assert.Equal(true, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldEvaluateAndFunctionWithOneFalseInMultipleArguments()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Value = true;
        sheet.Cells["A4"].Formula = "=AND(A1,A2,A3)";

        Assert.Equal(false, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldReturnValueErrorForEmptyAndFunction()
    {
        sheet.Cells["A1"].Formula = "=AND()";

        Assert.Equal(CellError.Value, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldEvaluateAndFunctionWithRangeExpression()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Value = true;
        sheet.Cells["A3"].Formula = "=AND(A1:A2)";

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateAndFunctionWithMixedTypes()
    {
        sheet.Cells["A1"].Value = 5;
        sheet.Cells["A2"].Value = "3";
        sheet.Cells["A3"].Value = true;
        sheet.Cells["A4"].Formula = "=AND(A1,A2,A3)";

        Assert.Equal(true, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldEvaluateAndFunctionInIfStatement()
    {
        sheet.Cells["A1"].Value = 5;
        sheet.Cells["A2"].Value = 10;
        sheet.Cells["A3"].Formula = "=IF(AND(A1>1,A2<100),A1,\"Out of range\")";

        Assert.Equal(5d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateAndFunctionInIfStatementWithFalseCondition()
    {
        sheet.Cells["A1"].Value = 5;
        sheet.Cells["A2"].Value = 150;
        sheet.Cells["A3"].Formula = "=IF(AND(A1>1,A2<100),A1,\"Out of range\")";

        Assert.Equal("Out of range", sheet.Cells["A3"].Value);
    }
}


