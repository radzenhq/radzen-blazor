using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class IfFunctionTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void ShouldEvaluateIfFunctionWithTrueCondition()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Formula = "=IF(A1=1,\"Yes\",\"No\")";

        Assert.Equal("Yes", sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfFunctionWithFalseCondition()
    {
        sheet.Cells["A1"].Value = 2;
        sheet.Cells["A2"].Formula = "=IF(A1=1,\"Yes\",\"No\")";

        Assert.Equal("No", sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfFunctionWithNumericComparison()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Formula = "=IF(A1>A2,\"Over Budget\",\"Within Budget\")";

        Assert.Equal("Over Budget", sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfFunctionWithNumericResult()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Formula = "=IF(A1>A2,A1-A2,0)";

        Assert.Equal(5d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfFunctionWithTwoArguments()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Formula = "=IF(A1=1,\"True\")";

        Assert.Equal("True", sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfFunctionWithTwoArgumentsFalseCondition()
    {
        sheet.Cells["A1"].Value = 0;
        sheet.Cells["A2"].Formula = "=IF(A1=1,\"True\")";

        Assert.Equal(false, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfFunctionWithZeroAsFalse()
    {
        sheet.Cells["A1"].Value = 0;
        sheet.Cells["A2"].Formula = "=IF(A1,\"True\",\"False\")";

        Assert.Equal("False", sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfFunctionWithNonZeroAsTrue()
    {
        sheet.Cells["A1"].Value = 5;
        sheet.Cells["A2"].Formula = "=IF(A1,\"True\",\"False\")";

        Assert.Equal("True", sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateToErrorIfFunctionWithStringCondition()
    {
        sheet.Cells["A1"].Value = "test";
        sheet.Cells["A2"].Formula = "=IF(A1,\"Not Empty\",\"Empty\")";

        Assert.Equal(CellError.Value, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfFunctionWithEmptyStringCondition()
    {
        sheet.Cells["A2"].Formula = "=IF(A1,\"Not Empty\",\"Empty\")";

        Assert.Equal("Empty", sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfFunctionWithNullCondition()
    {
        sheet.Cells["A2"].Formula = "=IF(A1,\"Not Empty\",\"Empty\")";

        Assert.Equal("Empty", sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldReturnValueErrorForTooFewArguments()
    {
        sheet.Cells["A1"].Formula = "=IF(A2)";

        Assert.Equal(CellError.Value, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldReturnValueErrorForTooManyArguments()
    {
        sheet.Cells["A1"].Formula = "=IF(A2,\"True\",\"False\",\"Extra\")";

        Assert.Equal(CellError.Value, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldPropagateErrorFromCondition()
    {
        sheet.Cells["A1"].Formula = "=IF(A6,\"True\",\"False\")";

        Assert.Equal(CellError.Ref, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldPropagateErrorFromTrueValue()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Formula = "=IF(A1=1,A6,\"False\")";

        Assert.Equal(CellError.Ref, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldPropagateErrorFromFalseValue()
    {
        sheet.Cells["A1"].Value = 0;
        sheet.Cells["A2"].Formula = "=IF(A1=1,\"True\",A6)";

        Assert.Equal(CellError.Ref, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateNestedIfFunction()
    {
        sheet.Cells["A1"].Value = 85;
        sheet.Cells["A2"].Formula = "=IF(A1>=90,\"A\",IF(A1>=80,\"B\",IF(A1>=70,\"C\",\"F\")))";

        Assert.Equal("B", sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfFunctionWithBooleanValues()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Formula = "=IF(A1,\"True\",\"False\")";

        Assert.Equal("True", sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfFunctionWithDecimalValues()
    {
        sheet.Cells["A1"].Value = 0.5m;
        sheet.Cells["A2"].Formula = "=IF(A1,\"True\",\"False\")";

        Assert.Equal("True", sheet.Cells["A2"].Value);
    }
}


