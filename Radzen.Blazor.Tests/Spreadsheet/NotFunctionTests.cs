using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class NotFunctionTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void ShouldEvaluateNotFunctionWithTrueValue()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Formula = "=NOT(A1)";

        Assert.Equal(false, sheet.Cells["A2"].Value);
    }
    
    [Fact]
    public void ShouldEvaluateNotFunctionWithEmptyStringAsError()
    {
        sheet.Cells["A1"].Value = "";
        sheet.Cells["A2"].Formula = "=NOT(A1)";

        Assert.Equal(CellError.Value, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateNotFunctionWithFalseValue()
    {
        sheet.Cells["A1"].Value = false;
        sheet.Cells["A2"].Formula = "=NOT(A1)";

        Assert.Equal(true, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateNotFunctionWithNumericValue()
    {
        sheet.Cells["A1"].Value = 5;
        sheet.Cells["A2"].Formula = "=NOT(A1)";

        Assert.Equal(false, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateNotFunctionWithZeroValue()
    {
        sheet.Cells["A1"].Value = 0;
        sheet.Cells["A2"].Formula = "=NOT(A1)";

        Assert.Equal(true, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateNotFunctionWithStringValue()
    {
        sheet.Cells["A1"].Value = "test";
        sheet.Cells["A2"].Formula = "=NOT(A1)";

        Assert.Equal(CellError.Value, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateNotFunctionWithEmptyValue()
    {
        sheet.Cells["A2"].Formula = "=NOT(A1)";

        Assert.Equal(true, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateNotFunctionWithComparison()
    {
        sheet.Cells["A1"].Value = 50;
        sheet.Cells["A2"].Formula = "=NOT(A1>100)";

        Assert.Equal(true, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateNotFunctionWithTrueComparison()
    {
        sheet.Cells["A1"].Value = 150;
        sheet.Cells["A2"].Formula = "=NOT(A1>100)";

        Assert.Equal(false, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldReturnValueErrorForNotFunctionWithNoArguments()
    {
        sheet.Cells["A1"].Formula = "=NOT()";

        Assert.Equal(CellError.Value, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldReturnValueErrorForNotFunctionWithMultipleArguments()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Formula = "=NOT(A1,A2)";

        Assert.Equal(CellError.Value, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateNotFunctionWithRangeExpression()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Formula = "=NOT(A1:A1)";

        Assert.Equal(false, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateNotFunctionWithDecimalValue()
    {
        sheet.Cells["A1"].Value = 0.5m;
        sheet.Cells["A2"].Formula = "=NOT(A1)";

        Assert.Equal(false, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateNotFunctionWithNegativeValue()
    {
        sheet.Cells["A1"].Value = -5;
        sheet.Cells["A2"].Formula = "=NOT(A1)";

        Assert.Equal(false, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateNotFunctionInIfStatement()
    {
        sheet.Cells["A1"].Value = 50;
        sheet.Cells["A2"].Formula = "=IF(NOT(A1>100),\"Valid\",\"Invalid\")";

        Assert.Equal("Valid", sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateNotFunctionInIfStatementWithFalseCondition()
    {
        sheet.Cells["A1"].Value = 150;
        sheet.Cells["A2"].Formula = "=IF(NOT(A1>100),\"Valid\",\"Invalid\")";

        Assert.Equal("Invalid", sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateNotFunctionWithProvidedExample1()
    {
        sheet.Cells["A2"].Value = 50;
        sheet.Cells["A3"].Formula = "=NOT(A2>100)";

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateNotFunctionWithProvidedExample2()
    {
        sheet.Cells["A2"].Value = 50;
        sheet.Cells["A3"].Formula = "=IF(AND(NOT(A2>1),NOT(A2<100)),A2,\"The value is out of range\")";

        Assert.Equal("The value is out of range", sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateNotFunctionWithProvidedExample3()
    {
        sheet.Cells["A3"].Value = 100;
        sheet.Cells["A4"].Formula = "=IF(OR(NOT(A3<0),NOT(A3>50)),A3,\"The value is out of range\")";

        Assert.Equal(100d, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldEvaluateNotFunctionWithNestedLogicalFunctions()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Formula = "=NOT(AND(A1,A2))";

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateNotFunctionWithOrFunction()
    {
        sheet.Cells["A1"].Value = false;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Formula = "=NOT(OR(A1,A2))";

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }
}


