using System;
using System.Linq.Expressions;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class FormulaEvaluationTests
{
    readonly Sheet sheet = new(5, 5);

    [Fact]
    public void ShouldEvaluateFormulaAfterSettingIt()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Formula = "=A1+1";

        Assert.Equal(2d, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateFormulaAfterSettingValue()
    {
        sheet.Cells["A1"].Formula = "=A2+1";
        sheet.Cells["A2"].Value = 1;

        Assert.Equal(2d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldNotEvaluateFormulaIfEditing()
    {
        sheet.BeginUpdate();
        sheet.Cells["A1"].Formula = "=A2+1";
        sheet.Cells["A2"].Value = 1;

        Assert.Null(sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldEvaluateFormulaAfterEndingEdit()
    {
        sheet.BeginUpdate();
        sheet.Cells["A1"].Formula = "=A2+1";
        sheet.Cells["A2"].Value = 1;
        sheet.EndUpdate();

        Assert.Equal(2d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldSetCellValueToErrorValueIfStringIsUsedInBinaryOperation()
    {
        sheet.Cells["A1"].Formula = "=A2+1";
        sheet.Cells["A2"].Value = "test";

        Assert.Equal(CellError.Value, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldEvaluateFormulaWhenDependencyIsChanged()
    {
        sheet.Cells["A1"].Formula = "=A2+1";
        sheet.Cells["A2"].Formula = "=A3+1";
        sheet.Cells["A3"].Value = 1;

        Assert.Equal(3d, sheet.Cells["A1"].Value);
        Assert.Equal(2d, sheet.Cells["A2"].Value);
        Assert.Equal(1d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateFormulaWhenDependencyIsChangedAndEndEditIsCalled()
    {
        sheet.BeginUpdate();
        sheet.Cells["A1"].Formula = "=A2+1";
        sheet.Cells["A2"].Formula = "=A3+1";
        sheet.Cells["A3"].Value = 1;
        sheet.EndUpdate();

        Assert.Equal(3d, sheet.Cells["A1"].Value);
        Assert.Equal(2d, sheet.Cells["A2"].Value);
        Assert.Equal(1d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldTreatEmptyValueAsZeroInFormula()
    {
        sheet.Cells["A1"].Formula = "=A2+1";

        Assert.Equal(1d, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldHandleSelfReferencingFormulas()
    {
        sheet.Cells["A1"].Formula = "=A1+1";

        // Setting a value should not cause infinite recursion
        sheet.Cells["A1"].Value = 1;

        // The value should be stable and not cause infinite recursion
        Assert.NotNull(sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldSetDiv0ErrorWhenDividingByZero()
    {
        sheet.Cells["A1"].Formula = "=A2/A3";
        sheet.Cells["A2"].Value = 1;
        sheet.Cells["A3"].Value = 0;

        Assert.Equal(CellError.Div0, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldSetErrorToCircularWhenCellFormulasReferenceEachOther()
    {
        sheet.Cells["A1"].Formula = "=A2+1";
        sheet.Cells["A2"].Formula = "=A1+1";

        // The value should be an error
        Assert.Equal(CellError.Circular, sheet.Cells["A1"].Value);
        Assert.Equal(CellError.Circular, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateSumFunction()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 2;
        sheet.Cells["A3"].Formula = "=SUM(A1,A2)";

        Assert.Equal(3d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateSumFunctionWithEmptyCells()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A3"].Formula = "=SUM(A1,A2)";

        Assert.Equal(1d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateSumFunctionWithMultipleArguments()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 2;
        sheet.Cells["A3"].Value = 3;
        sheet.Cells["A4"].Formula = "=SUM(A1,A2,A3)";

        Assert.Equal(6d, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldReturnValueErrorForEmptySumFunction()
    {
        sheet.Cells["A1"].Formula = "=SUM()";

        Assert.Equal(CellError.Value, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldReturnNameErrorForUnknownFunction()
    {
        sheet.Cells["A1"].Formula = "=UNKNOWN()";

        Assert.Equal(CellError.Name, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldSumRangeOfCells()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 2;
        sheet.Cells["A3"].Formula = "=SUM(A1:A2)";

        Assert.Equal(3d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldSumNumbersOfDifferentTypes()
    {
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 2.5;
        sheet.Cells["A3"].Formula = "=SUM(A1,A2)";

        Assert.Equal(3.5, sheet.Cells["A3"].Value);

        sheet.Cells["A4"].Value = 2.5;
        sheet.Cells["A5"].Formula = "=SUM(A4,A1)";

        Assert.Equal(3.5, sheet.Cells["A5"].Value);
    }

    [Fact]
    public void ShouldCreateRefErrorWhenOutOfBounds()
    {
        sheet.Cells["A1"].Formula = "=A6";

        Assert.Equal(CellError.Ref, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldCreateRefErrorWhenRangeOutOfBounds()
    {
        sheet.Cells["A1"].Formula = "=SUM(A2:A6)";

        Assert.Equal(CellError.Ref, sheet.Cells["A1"].Value);
    }

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

        // The COUNT function should count all numeric values
        Assert.Equal(3.0, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void ShouldCreateRefErrorWhenCountRangeOutOfBounds()
    {
        sheet.Cells["A1"].Formula = "=COUNT(A2:A6)";

        Assert.Equal(CellError.Ref, sheet.Cells["A1"].Value);
    }

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
    public void ShouldEvaluateIfFunctionWithStringCondition()
    {
        sheet.Cells["A1"].Value = "test";
        sheet.Cells["A2"].Formula = "=IF(A1,\"Not Empty\",\"Empty\")";

        Assert.Equal("Not Empty", sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateIfFunctionWithEmptyStringCondition()
    {
        sheet.Cells["A1"].Value = "";
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

    [Fact]
    public void ShouldReturnNameErrorForUnknownFunctionUppercase()
    {
        sheet.Cells["A1"].Formula = "=UNKNOWNFUNCTION(1,2,3)";

        Assert.Equal(CellError.Name, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldReturnNameErrorForUnknownFunctionWithMixedCase()
    {
        sheet.Cells["A1"].Formula = "=UnknownFunction(1,2,3)";

        Assert.Equal(CellError.Name, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldReturnNameErrorForUnknownFunctionWithLowercase()
    {
        sheet.Cells["A1"].Formula = "=unknownfunction(1,2,3)";

        Assert.Equal(CellError.Name, sheet.Cells["A1"].Value);
    }

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

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateAndFunctionWithEmptyStringAsFalse()
    {
        sheet.Cells["A1"].Value = "";
        sheet.Cells["A2"].Value = "hello";
        sheet.Cells["A3"].Formula = "=AND(A1,A2)";

        Assert.Equal(false, sheet.Cells["A3"].Value);
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
        sheet.Cells["A2"].Value = "test";
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

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithEmptyStringAsFalse()
    {
        sheet.Cells["A1"].Value = "";
        sheet.Cells["A2"].Value = "hello";
        sheet.Cells["A3"].Formula = "=OR(A1,A2)";

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithBothEmptyStringsAsFalse()
    {
        sheet.Cells["A1"].Value = "";
        sheet.Cells["A2"].Value = "";
        sheet.Cells["A3"].Formula = "=OR(A1,A2)";

        Assert.Equal(false, sheet.Cells["A3"].Value);
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

    [Fact]
    public void ShouldEvaluateNotFunctionWithTrueValue()
    {
        sheet.Cells["A1"].Value = true;
        sheet.Cells["A2"].Formula = "=NOT(A1)";

        Assert.Equal(false, sheet.Cells["A2"].Value);
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

        Assert.Equal(false, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void ShouldEvaluateNotFunctionWithEmptyStringValue()
    {
        sheet.Cells["A1"].Value = "";
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