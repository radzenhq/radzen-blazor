using System;
using System.Linq.Expressions;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class FormulaEvaluationTests
{
    readonly Worksheet sheet = new(5, 5);

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

    [Theory]
    [InlineData("=1+¤")]
    [InlineData("=1 ¤ 2")]
    [InlineData("=SUM(")]
    public void ShouldSetCellValueToErrorNameIfReferencedFormulaDoesNotParse(string formula)
    {
        sheet.Cells["A1"].Formula = formula;
        sheet.Cells["B1"].Formula = "=A1+1";
        sheet.Cells["C1"].Formula = "=SUM(A1:A2)";

        Assert.Equal(CellError.Name, sheet.Cells["A1"].Value);
        Assert.Equal(CellError.Name, sheet.Cells["B1"].Value);
        Assert.Equal(CellError.Name, sheet.Cells["C1"].Value);
    }

    [Theory]
    [InlineData("=1+¤")]
    [InlineData("=1 ¤ 2")]
    [InlineData("=SUM(")]
    public void ShouldSetCellValueToErrorNameIfReferencedFormulaThatDoesNotParseIsSetLast(string formula)
    {
        sheet.Cells["B1"].Formula = "=A1+1";
        sheet.Cells["C1"].Formula = "=SUM(A1:A2)";
        sheet.Cells["A1"].Formula = formula;

        Assert.Equal(CellError.Name, sheet.Cells["A1"].Value);
        Assert.Equal(CellError.Name, sheet.Cells["B1"].Value);
        Assert.Equal(CellError.Name, sheet.Cells["C1"].Value);
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
    public void ShouldEvaluateMutuallyReferencingRangesWithoutExponentialBlowup()
    {
        var sheet = new Worksheet(10, 2);

        sheet.BeginUpdate();

        for (var row = 1; row <= 10; row++)
        {
            sheet.Cells[$"A{row}"].Formula = "=SUM(B1:B10)";
            sheet.Cells[$"B{row}"].Formula = "=SUM(A1:A10)";
        }

        sheet.EndUpdate();

        Assert.Equal(CellError.Circular, sheet.Cells["A1"].Value);
        Assert.Equal(CellError.Circular, sheet.Cells["B10"].Value);
    }

    [Fact]
    public void UnaryPlusShouldReturnTextUnchanged()
    {
        sheet.Cells["A1"].Value = "abc";
        sheet.Cells["A2"].Formula = "=+A1";
        sheet.Cells["A3"].Formula = "=+\"abc\"";

        Assert.Equal("abc", sheet.Cells["A2"].Value);
        Assert.Equal("abc", sheet.Cells["A3"].Value);
    }

    [Fact]
    public void UnaryPlusShouldKeepEmptyCellEmptySoConcatenationDoesNotProduceZero()
    {
        sheet.Cells["A2"].Formula = "=+A1";
        sheet.Cells["A3"].Formula = "=+A1&+B1";
        sheet.Cells["A4"].Formula = "=+A1+1";

        Assert.Equal(0d, sheet.Cells["A2"].Value);
        Assert.Equal("", sheet.Cells["A3"].Value);
        Assert.Equal(1d, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void IfShouldIgnoreErrorsInTheBranchThatIsNotTaken()
    {
        sheet.Cells["A1"].Formula = "=IF(1=1,\"\",1/0)";
        sheet.Cells["A2"].Formula = "=IF(1=0,1/0,\"ok\")";
        sheet.Cells["A3"].Formula = "=IF(1=1,1/0,\"ok\")";
        sheet.Cells["A4"].Formula = "=IF(1/0,1,2)";

        Assert.Equal("", sheet.Cells["A1"].Value);
        Assert.Equal("ok", sheet.Cells["A2"].Value);
        Assert.Equal(CellError.Div0, sheet.Cells["A3"].Value);
        Assert.Equal(CellError.Div0, sheet.Cells["A4"].Value);
    }

    [Fact]
    public void NaShouldReturnTheNotAvailableError()
    {
        sheet.Cells["A1"].Formula = "=NA()";
        sheet.Cells["A2"].Formula = "=IFERROR(NA(),\"caught\")";

        Assert.Equal(CellError.NA, sheet.Cells["A1"].Value);
        Assert.Equal("caught", sheet.Cells["A2"].Value);
    }

    [Fact]
    public void AmpersandShouldConcatenateValuesAsText()
    {
        sheet.Cells["A1"].Value = "abc";
        sheet.Cells["A2"].Value = 5;
        sheet.Cells["A3"].Value = true;
        sheet.Cells["B1"].Formula = "=A1&A2";
        sheet.Cells["B2"].Formula = "=A2&A4";
        sheet.Cells["B3"].Formula = "=+A1&+A2";
        sheet.Cells["B4"].Formula = "=A1&\" \"&A3";

        Assert.Equal("abc5", sheet.Cells["B1"].Value);
        Assert.Equal("5", sheet.Cells["B2"].Value);
        Assert.Equal("abc5", sheet.Cells["B3"].Value);
        Assert.Equal("abc TRUE", sheet.Cells["B4"].Value);
    }

    [Fact]
    public void AmpersandShouldBindLooserThanArithmeticAndTighterThanComparison()
    {
        sheet.Cells["A1"].Formula = "=\"a\"&1+2";
        sheet.Cells["A2"].Formula = "=\"a\"&\"b\"=\"ab\"";
        sheet.Cells["A3"].Formula = "=1&2*3";

        Assert.Equal("a3", sheet.Cells["A1"].Value);
        Assert.Equal(true, sheet.Cells["A2"].Value);
        Assert.Equal("16", sheet.Cells["A3"].Value);
    }

    [Fact]
    public void AmpersandShouldPropagateErrors()
    {
        sheet.Cells["A1"].Formula = "=\"a\"&1/0";

        Assert.Equal(CellError.Div0, sheet.Cells["A1"].Value);
    }

    [Fact]
    public void ArithmeticShouldCoerceNumericTextLikeExcel()
    {
        sheet.Cells["A1"].SetText("45");
        sheet.Cells["A2"].Value = "abc";
        sheet.Cells["B1"].Formula = "=A1+1";
        sheet.Cells["B2"].Formula = "=\"3\"*\"4\"";
        sheet.Cells["B3"].Formula = "=-A1";
        sheet.Cells["B4"].Formula = "=A2+1";

        Assert.Equal(46d, sheet.Cells["B1"].Value);
        Assert.Equal(12d, sheet.Cells["B2"].Value);
        Assert.Equal(-45d, sheet.Cells["B3"].Value);
        Assert.Equal(CellError.Value, sheet.Cells["B4"].Value);
    }

    [Fact]
    public void EmptyCellShouldCompareEqualToEmptyStringAndZero()
    {
        sheet.Cells["B1"].Formula = "=A1=\"\"";
        sheet.Cells["B2"].Formula = "=A1=0";
        sheet.Cells["B3"].Formula = "=A1<>\"\"";
        sheet.Cells["B4"].Formula = "=IF(A1=\"\",\"empty\",\"filled\")";
        sheet.Cells["B5"].Formula = "=A1=FALSE";

        Assert.Equal(true, sheet.Cells["B1"].Value);
        Assert.Equal(true, sheet.Cells["B2"].Value);
        Assert.Equal(false, sheet.Cells["B3"].Value);
        Assert.Equal("empty", sheet.Cells["B4"].Value);
        Assert.Equal(true, sheet.Cells["B5"].Value);
    }

    [Fact]
    public void MatchWithEmptyLookupValueReturnsNotAvailable()
    {
        sheet.Cells["A1"].Value = "x";
        sheet.Cells["A2"].Value = "";
        sheet.Cells["B1"].Formula = "=MATCH(C1,A1:A3,0)";
        sheet.Cells["B2"].Formula = "=MATCH(C1,A1:A3,1)";

        Assert.Equal(CellError.NA, sheet.Cells["B1"].Value);
        Assert.Equal(CellError.NA, sheet.Cells["B2"].Value);
    }

    [Fact]
    public void MatchSkipsErrorCellsInTheLookupArray()
    {
        sheet.Cells["A1"].Value = "a";
        sheet.Cells["A2"].Formula = "=1/0";
        sheet.Cells["A3"].Value = "x";
        sheet.Cells["B1"].Formula = "=MATCH(\"x\",A1:A3,0)";
        sheet.Cells["B2"].Formula = "=MATCH(\"z\",A1:A3,0)";

        Assert.Equal(3d, sheet.Cells["B1"].Value);
        Assert.Equal(CellError.NA, sheet.Cells["B2"].Value);
    }

    [Fact]
    public void FormulaReturningAnEmptyCellDisplaysZero()
    {
        sheet.Cells["B1"].Formula = "=A1";
        sheet.Cells["B2"].Formula = "=IF(TRUE,A1,1)";
        sheet.Cells["B3"].Formula = "=INDEX(A1:A3,2)";
        sheet.Cells["B4"].Formula = "=A1&\"\"";

        Assert.Equal(0d, sheet.Cells["B1"].Value);
        Assert.Equal(0d, sheet.Cells["B2"].Value);
        Assert.Equal(0d, sheet.Cells["B3"].Value);
        Assert.Equal("", sheet.Cells["B4"].Value);
    }

    [Fact]
    public void ShouldReturnNameErrorForUnknownFunction()
    {
        sheet.Cells["A1"].Formula = "=UNKNOWN()";

        Assert.Equal(CellError.Name, sheet.Cells["A1"].Value);
    }

    [Theory]
    [InlineData("=A6", 0d)]
    [InlineData("=F1", 0d)]
    [InlineData("=A6+A2", 2d)]
    [InlineData("=SUM(A2:A100)", 14d)]
    [InlineData("=COUNT(A2:A100)", 4d)]
    [InlineData("=COUNTA(A2:Z6)", 4d)]
    [InlineData("=COUNTBLANK(A2:A100)", 95d)]
    [InlineData("=ROWS(A2:A100)", 99d)]
    [InlineData("=INDEX(A2:A100,50)", 0d)]
    [InlineData("=COUNTBLANK(A1:A1048576)", 1048572d)]
    [InlineData("=SUM(A2:XFD1048576)", 14d)]
    public void ShouldReadCellsPastTheGridAsEmptyCells(string formula, double expected)
    {
        sheet.Cells["A2"].Value = 2;
        sheet.Cells["A3"].Value = 3;
        sheet.Cells["A4"].Value = 4;
        sheet.Cells["A5"].Value = 5;
        sheet.Cells["B1"].Formula = formula;

        Assert.Equal(expected, sheet.Cells["B1"].Value);
    }

    [Theory]
    [InlineData("=A1048577")]
    [InlineData("=XFE1")]
    [InlineData("=SUM(A1:A1048577)")]
    public void ShouldSetCellValueToErrorNameForReferencePastTheLastRowOrColumn(string formula)
    {
        sheet.Cells["B1"].Formula = formula;

        Assert.Equal(CellError.Name, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void Evaluator_ShouldReadCellsPastTheGridOfAnotherSheetAsEmptyCells()
    {
        var wb = new Workbook();
        var s1 = wb.AddSheet("Worksheet1", 5, 5);
        var s2 = wb.AddSheet("Worksheet2", 3, 3);

        s2.Cells[0, 0].Value = 7;

        s1.Cells[0, 0].Formula = "=SUM(Worksheet2!A1:A100)+Worksheet2!Z99";

        Assert.Equal(7d, s1.Cells[0, 0].Value);
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

    [Fact]
    public void ShouldRecalculateRangeFormulaWhenMiddleCellChanges()
    {
        var ws = new Worksheet(10, 5);
        ws.Cells["A1"].Value = 1;
        ws.Cells["A2"].Value = 2;
        ws.Cells["A3"].Value = 3;
        ws.Cells["A4"].Value = 4;
        ws.Cells["A5"].Value = 5;
        ws.Cells["B1"].Formula = "=SUM(A1:A5)";

        Assert.Equal(15d, ws.Cells["B1"].Value);

        ws.Cells["A3"].Value = 10;

        Assert.Equal(22d, ws.Cells["B1"].Value);
    }

    [Fact]
    public void ShouldRecalculateRangeFormulaWhenAnyCellInRangeChanges()
    {
        var ws = new Worksheet(10, 5);
        ws.Cells["A1"].Value = 1;
        ws.Cells["A2"].Value = 2;
        ws.Cells["A3"].Value = 3;
        ws.Cells["A4"].Value = 4;
        ws.Cells["A5"].Value = 5;
        ws.Cells["B1"].Formula = "=SUM(A1:A5)";

        Assert.Equal(15d, ws.Cells["B1"].Value);

        ws.Cells["A2"].Value = 20;
        Assert.Equal(33d, ws.Cells["B1"].Value);

        ws.Cells["A4"].Value = 40;
        Assert.Equal(69d, ws.Cells["B1"].Value);

        ws.Cells["A1"].Value = 10;
        Assert.Equal(78d, ws.Cells["B1"].Value);

        ws.Cells["A5"].Value = 50;
        Assert.Equal(123d, ws.Cells["B1"].Value);
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
        var s1 = wb.AddSheet("Worksheet1", 5, 5);
        var s2 = wb.AddSheet("Worksheet2", 5, 5);

        s2.Cells[0, 2].Value = 42; // C1 on Worksheet2

        s1.Cells[0, 0].Formula = "=Worksheet2!C1"; // A1 on Worksheet1 refers to Worksheet2!C1

        Assert.Equal(42d, s1.Cells[0, 0].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Evaluator_ShouldResolveCrossSheetRangeInFunction()
    {
        var wb = new Workbook();
        var s1 = wb.AddSheet("Worksheet1", 5, 5);
        var s2 = wb.AddSheet("Worksheet2", 5, 5);

        s2.Cells[0, 0].Value = 1; // A1
        s2.Cells[0, 1].Value = 2; // B1
        s2.Cells[1, 0].Value = 3; // A2
        s2.Cells[1, 1].Value = 4; // B2

        s1.Cells[0, 0].Formula = "=SUM(Worksheet2!A1:Worksheet2!B2)";

        Assert.Equal(10d, s1.Cells[0, 0].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Evaluator_ShouldResolveQuotedSheetCellReference()
    {
        var wb = new Workbook();
        var s1 = wb.AddSheet("Summary", 5, 5);
        var s2 = wb.AddSheet("Q1 Sales", 5, 5);

        s2.Cells[0, 0].Value = 100;

        s1.Cells[0, 0].Formula = "='Q1 Sales'!A1";

        Assert.Equal(100d, s1.Cells[0, 0].Data.GetValueOrDefault<double>());
    }

    [Fact]
    public void Evaluator_ShouldResolveQuotedSheetRangeInFunction()
    {
        var wb = new Workbook();
        var s1 = wb.AddSheet("Summary", 5, 5);
        var s2 = wb.AddSheet("Q1 Sales", 5, 5);

        s2.Cells[0, 1].Value = 10; // B1
        s2.Cells[0, 2].Value = 20; // C1
        s2.Cells[0, 3].Value = 30; // D1

        s1.Cells[0, 0].Formula = "=SUM('Q1 Sales'!B1:D1)";

        Assert.Equal(60d, s1.Cells[0, 0].Data.GetValueOrDefault<double>());
    }

    [Theory]
    [InlineData("入荷実績")]
    [InlineData("Übersicht")]
    [InlineData("𠀋表")]
    public void Evaluator_ShouldResolveUnquotedSheetRangeOutsideAscii(string sheetName)
    {
        var wb = new Workbook();
        var data = wb.AddSheet(sheetName, 5, 5);
        var report = wb.AddSheet("Report", 5, 5);

        data.Cells[0, 0].Value = "A";
        data.Cells[0, 1].Value = 10;
        data.Cells[1, 0].Value = "B";
        data.Cells[1, 1].Value = 20;
        data.Cells[2, 0].Value = "A";
        data.Cells[2, 1].Value = 5;

        report.Cells[0, 0].Value = "A";
        report.Cells[0, 1].Formula = $"=SUMIF({sheetName}!$A$1:$A$3,A1,{sheetName}!$B$1:$B$3)";
        report.Cells[1, 1].Formula = "=B1*2";

        Assert.Equal(15d, report.Cells[0, 1].Value);
        Assert.Equal(30d, report.Cells[1, 1].Value);
    }

    [Fact]
    public void Evaluator_ShouldResolveUnquotedSheetReferenceOutsideAsciiAfterXlsxRoundTrip()
    {
        var wb = new Workbook();
        var data = wb.AddSheet("入荷実績", 5, 5);
        var report = wb.AddSheet("Report", 5, 5);

        data.Cells[0, 0].Value = 7;
        report.Cells[0, 0].Formula = "=入荷実績!A1*2";
        report.Cells[0, 1].Formula = "=A1+1";

        using var stream = new System.IO.MemoryStream();
        wb.SaveToStream(stream);
        stream.Position = 0;

        var loaded = Workbook.LoadFromStream(stream).GetSheet("Report")!;

        Assert.Equal("=入荷実績!A1*2", loaded.Cells[0, 0].Formula);
        Assert.Equal(14d, loaded.Cells[0, 0].Value);
        Assert.Equal(15d, loaded.Cells[0, 1].Value);
    }
}