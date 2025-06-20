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
}