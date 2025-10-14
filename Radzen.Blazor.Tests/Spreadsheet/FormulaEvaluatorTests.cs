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
    public void ShouldSetCellValueToErrorNameIfInvalidFunctionIsUsedInFormula()
    {
        sheet.Cells["A1"].Formula = "=INVALID_FUNCTION()";
        sheet.Cells["A2"].Value = "test";

        Assert.Equal(CellError.Name, sheet.Cells["A1"].Value);
    }

    [Theory]
    [InlineData("=SUM(")]
    [InlineData("=SUM(A2,")]
    [InlineData("=SUM(A2:A2")]
    public void ShouldSetCellValueToErrorNameIfIncompleteFunctionIsUsedInFormula(string formula)
    {
        sheet.Cells["A1"].Formula = formula;
        sheet.Cells["A2"].Value = "test";

        Assert.Equal(CellError.Name, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldSetCellValueToEqualsIfOnlyEqualsIsSetAsFormula()
    {
        sheet.Cells["A1"].SetValue("=");

        Assert.Equal("=", sheet.Cells["A1"].Value);
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
    public void ShouldReturnNameErrorForUnknownFunction()
    {
        sheet.Cells["A1"].Formula = "=UNKNOWN()";

        Assert.Equal(CellError.Name, sheet.Cells["A1"].Value);
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
    public void ShouldCreateRefErrorWhenCountRangeOutOfBounds()
    {
        sheet.Cells["A1"].Formula = "=COUNT(A2:A6)";

        Assert.Equal(CellError.Ref, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ShouldCreateRefErrorWhenCountaRangeOutOfBounds()
    {
        sheet.Cells["A1"].Formula = "=COUNTA(A2:A6)";

        Assert.Equal(CellError.Ref, sheet.Cells["A1"].Value);
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
    public void ShouldEvaluateAndFunctionInIfStatementWithFalseCondition()
    {
        sheet.Cells["A1"].Value = 5;
        sheet.Cells["A2"].Value = 150;
        sheet.Cells["A3"].Formula = "=IF(AND(A1>1,A2<100),A1,\"Out of range\")";

        Assert.Equal("Out of range", sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateOrFunctionWithProvidedExample3()
    {
        sheet.Cells["A2"].Value = 75;
        sheet.Cells["A3"].Formula = "=IF(OR(A2<0,A2>50),A2,\"The value is out of range\")";

        Assert.Equal(75d, sheet.Cells["A3"].Value);
    }

    [Fact]
    public void ShouldEvaluateNotFunctionWithOrFunction()
    {
        sheet.Cells["A1"].Value = false;
        sheet.Cells["A2"].Value = false;
        sheet.Cells["A3"].Formula = "=NOT(OR(A1,A2))";

        Assert.Equal(true, sheet.Cells["A3"].Value);
    }

    // IFERROR function tests are in IfErrorFunctionTests.cs

    [Fact]
    public void ShouldEvaluateSimpleDivisionByZero()
    {
        sheet.Cells["A1"].Value = 10;
        sheet.Cells["A2"].Formula = "=A1/0";

        Assert.Equal(CellError.Div0, sheet.Cells["A2"].Value);
    }

    [Fact]
    public void Evaluator_ShouldResolveCrossSheetCellReference()
    {
        var wb = new Workbook();
        var s1 = wb.AddSheet("Sheet1", 5, 5);
        var s2 = wb.AddSheet("Sheet2", 5, 5);

        s2.Cells[0, 2].Value = 42; // C1 on Sheet2

        s1.Cells[0, 0].Formula = "=Sheet2!C1"; // A1 on Sheet1 refers to Sheet2!C1

        Assert.Equal(42d, s1.Cells[0, 0].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Evaluator_ShouldResolveCrossSheetRangeInFunction()
    {
        var wb = new Workbook();
        var s1 = wb.AddSheet("Sheet1", 5, 5);
        var s2 = wb.AddSheet("Sheet2", 5, 5);

        s2.Cells[0, 0].Value = 1; // A1
        s2.Cells[0, 1].Value = 2; // B1
        s2.Cells[1, 0].Value = 3; // A2
        s2.Cells[1, 1].Value = 4; // B2

        s1.Cells[0, 0].Formula = "=SUM(Sheet2!A1:Sheet2!B2)";

        Assert.Equal(10d, s1.Cells[0, 0].Data.GetValueOrDefault<double>());
    }
}