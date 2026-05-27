using System.Reflection;
using Xunit;

using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet.Tests;

/// <summary>
/// Regression tests for <c>Worksheet.AdjustFormulaForCopy</c>. The previous lexer-based
/// implementation walked tokens individually and missed range references such as
/// <c>A1:B2</c>, so autofilling a formula like <c>=SUM(A1:B2)</c> did not shift the
/// range endpoints. The replacement parses the formula into a syntax tree and uses
/// <c>FormulaRewriter</c>, which already understands ranges and functions.
/// </summary>
public class AdjustFormulaForCopyTests
{
    private static string Adjust(string formula, int rowDelta, int colDelta)
    {
        var method = typeof(Worksheet).GetMethod(
            "AdjustFormulaForCopy",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, [formula, rowDelta, colDelta])!;
    }

    [Fact]
    public void RelativeCellReference_Adjusts()
    {
        Assert.Equal("=A2", Adjust("=A1", 1, 0));
    }

    [Fact]
    public void AbsoluteReference_DoesNotAdjust()
    {
        Assert.Equal("=$A$1", Adjust("=$A$1", 1, 1));
    }

    [Fact]
    public void MixedReference_AbsoluteColumn_OnlyRowAdjusts()
    {
        Assert.Equal("=$A2", Adjust("=$A1", 1, 1));
    }

    [Fact]
    public void MixedReference_AbsoluteRow_OnlyColumnAdjusts()
    {
        Assert.Equal("=B$1", Adjust("=A$1", 1, 1));
    }

    [Fact]
    public void RangeReference_Adjusts()
    {
        Assert.Equal("=SUM(A2:B3)", Adjust("=SUM(A1:B2)", 1, 0));
    }

    [Fact]
    public void RangeReference_AbsolutePartsRespected()
    {
        Assert.Equal("=SUM($A$1:C3)", Adjust("=SUM($A$1:B2)", 1, 1));
    }

    [Fact]
    public void FunctionCall_AdjustsArguments()
    {
        Assert.Equal("=IF(A2>0,B2,C2)", Adjust("=IF(A1>0, B1, C1)", 1, 0));
    }

    [Fact]
    public void CrossSheetReference_AdjustsCellNotSheetName()
    {
        Assert.Equal("=Sheet2!A2", Adjust("=Sheet2!A1", 1, 0));
    }

    [Fact]
    public void MalformedFormula_ReturnsUnchanged()
    {
        var input = "=A1+";
        var result = Adjust(input, 1, 0);
        Assert.Equal(input, result);
    }
}
