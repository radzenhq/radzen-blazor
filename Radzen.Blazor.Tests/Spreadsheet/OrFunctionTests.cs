using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class OrFunctionTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void ShouldEvaluateOrFunctionWithAllTrueValues()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Value = true;
        sheet.Cells["A3"].Formula = "=OR(A1,A2)";

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithOneTrueValue()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Formula = "=OR(A1,A2)";

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithAllFalseValues()
    {
        sheet.Cells["A1"].Value = false;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Formula = "=OR(A1,A2)";

        Assert.Equal(false, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithNumericValues()
    {
        sheet.Cells["A1"].Value = 5;
        sheet.Cells["A2"].Value = 10;
        sheet.Cells["A3"].Formula = "=OR(A1>1,A2<100)";

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithZeroAsFalse()
    {
        sheet.Cells["A1"].Value = 0;
        sheet.Cells["A2"].Value = 1;
        sheet.Cells["A3"].Formula = "=OR(A1,A2)";

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithBothZeroAsFalse()
    {
        sheet.Cells["A1"].Value = 0;
        sheet.Cells["A2"].Value = 0;
        sheet.Cells["A3"].Formula = "=OR(A1,A2)";

        Assert.Equal(false, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithNonZeroAsTrue()
    {
        sheet.Cells["A1"].Value = 5;
        sheet.Cells["A2"].Value = 10;
        sheet.Cells["A3"].Formula = "=OR(A1,A2)";

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithStringValues()
    {
        sheet.Cells["A1"].Value = "test";
        sheet.Cells["A2"].Value = "hello";
        sheet.Cells["A3"].Formula = "=OR(A1,A2)";

        Assert.Equal(CellError.Value, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithStringValueAndNumber()
    {
        sheet.Cells["A1"].Value = "2";
        sheet.Cells["A2"].Value = "hello";
        sheet.Cells["A3"].Formula = "=OR(A1,A2)";

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithEmptyStringAsFalse()
    {
        sheet.Cells["A2"].Value = "2";
        sheet.Cells["A3"].Formula = "=OR(A1,A2)";

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithBothEmptyStringsAsFalse()
    {
        sheet.Cells["A3"].Formula = "=OR(A1,A2)";

        Assert.Equal(CellError.Value, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithMultipleArguments()
    {
        sheet.Cells["A1"].Value = false;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Value = true;
        sheet.Cells["A4"].Formula = "=OR(A1,A2,A3)";

        Assert.Equal(true, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithAllFalseInMultipleArguments()
    {
        sheet.Cells["A1"].Value = false;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Value = false;
        sheet.Cells["A4"].Formula = "=OR(A1,A2,A3)";

        Assert.Equal(false, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldReturnValueErrorForEmptyOrFunction()
    {
        sheet.Cells["A1"].Formula = "=OR()";

        Assert.Equal(CellError.Value, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithRangeExpression()
    {
        sheet.Cells["A1"].Value = false;
        sheet.Cells["A2"].Value = true;
        sheet.Cells["A3"].Formula = "=OR(A1:A2)";

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithMixedTypes()
    {
        sheet.Cells["A1"].Value = 0;
        sheet.Cells["A2"].Value = "";
        sheet.Cells["A3"].Value = true;
        sheet.Cells["A4"].Formula = "=OR(A1,A2,A3)";

        Assert.Equal(true, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionInIfStatement()
    {
        sheet.Cells["A1"].Value = 5;
        sheet.Cells["A2"].Value = 10;
        sheet.Cells["A3"].Formula = "=IF(OR(A1>1,A2<100),A1,\"Out of range\")";

        Assert.Equal(5d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionInIfStatementWithFalseCondition()
    {
        sheet.Cells["A1"].Value = 0;
        sheet.Cells["A2"].Value = 150;
        sheet.Cells["A3"].Formula = "=IF(OR(A1>1,A2<100),A1,\"Out of range\")";

        Assert.Equal("Out of range", sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithProvidedExample1()
    {
        sheet.Cells["A2"].Value = 50;
        sheet.Cells["A3"].Formula = "=OR(A2>1,A2<100)";

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithProvidedExample2()
    {
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Value = 25;
        sheet.Cells["A4"].Formula = "=IF(OR(A2>1,A2<100),A3,\"The value is out of range\")";

        Assert.Equal(25d, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithProvidedExample3()
    {
        sheet.Cells["A2"].Value = 75;
        sheet.Cells["A3"].Formula = "=IF(OR(A2<0,A2>50),A2,\"The value is out of range\")";

        Assert.Equal(75d, sheet.Cells["A3"].Value);
    }
}


