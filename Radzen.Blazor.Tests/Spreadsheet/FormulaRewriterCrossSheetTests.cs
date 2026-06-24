using System.Reflection;
using Xunit;

using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class FormulaRewriterCrossSheetTests
{
    private static string Adjust(string formula, int rowDelta, int colDelta)
    {
        var method = typeof(Worksheet).GetMethod(
            "AdjustFormulaForCopy",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        return (string)method.Invoke(null, [formula, rowDelta, colDelta])!;
    }

    [Fact]
    public void CrossSheetCell_RowShift_PreservesSheetPrefix()
    {
        Assert.Equal("=Sheet2!A2", Adjust("=Sheet2!A1", 1, 0));
    }

    [Fact]
    public void CrossSheetRange_RowShift_PreservesSheetPrefixOnStartOnly()
    {
        Assert.Equal("=Sheet2!A2:B3", Adjust("=Sheet2!A1:B2", 1, 0));
    }

    [Fact]
    public void QuotedSheetName_RowShift_PreservesQuotedPrefix()
    {
        Assert.Equal("='My Sheet'!A2", Adjust("='My Sheet'!A1", 1, 0));
    }

    [Fact]
    public void CrossSheetAbsoluteReference_DoesNotAdjust()
    {
        Assert.Equal("=Sheet2!$A$1", Adjust("=Sheet2!$A$1", 1, 1));
    }

    [Fact]
    public void MultipleCrossSheetReferences_AllPreserveTheirPrefixes()
    {
        Assert.Equal(
            "=SUM(Sheet2!A2:B3,Sheet3!C4)",
            Adjust("=SUM(Sheet2!A1:B2, Sheet3!C3)", 1, 0));
    }

    [Fact]
    public void BareCell_NoPrefixAdded()
    {
        Assert.Equal("=A2", Adjust("=A1", 1, 0));
    }
}
