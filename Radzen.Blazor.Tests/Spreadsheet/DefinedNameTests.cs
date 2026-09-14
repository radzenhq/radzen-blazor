using System.IO;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

public class DefinedNameTests
{
    private static Workbook CreateWorkbook()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Plan input", 10, 5);
        sheet.Cells["A1"].Value = 1;
        sheet.Cells["A2"].Value = 2;
        sheet.Cells["A3"].Value = 3;
        sheet.Cells["B1"].Value = "H";
        sheet.Cells["B2"].Value = "D";
        sheet.Cells["B3"].Value = "H";
        workbook.DefinedNames["PLANhours"] = "'Plan input'!$A$1:$A$3";
        workbook.DefinedNames["PLANunit"] = "'Plan input'!$B$1:$B$3";
        workbook.DefinedNames["Rate"] = "$A$2";
        workbook.DefinedNames["Factor"] = "0.5";
        return workbook;
    }

    [Fact]
    public void NameReferringToARangeWorksAsAFunctionArgument()
    {
        var sheet = CreateWorkbook().Sheets[0];

        sheet.Cells["C1"].Formula = "=SUM(PLANhours)";
        sheet.Cells["C2"].Formula = "=SUMIFS(PLANhours,PLANunit,\"H\")";

        Assert.Equal(6d, sheet.Cells["C1"].Value);
        Assert.Equal(4d, sheet.Cells["C2"].Value);
    }

    [Fact]
    public void NameReferringToACellOrConstantWorksInExpressions()
    {
        var sheet = CreateWorkbook().Sheets[0];

        sheet.Cells["C1"].Formula = "=rate*10";
        sheet.Cells["C2"].Formula = "=Factor+1";

        Assert.Equal(20d, sheet.Cells["C1"].Value);
        Assert.Equal(1.5d, sheet.Cells["C2"].Value);
    }

    [Fact]
    public void FormulasUsingANameRecalculateWhenTheReferencedCellsChange()
    {
        var sheet = CreateWorkbook().Sheets[0];

        sheet.Cells["C1"].Formula = "=SUM(PLANhours)";
        sheet.Cells["A1"].Value = 10;

        Assert.Equal(15d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void UnknownNameReturnsNameError()
    {
        var sheet = CreateWorkbook().Sheets[0];

        sheet.Cells["C1"].Formula = "=SUM(Missing)";

        Assert.Equal(CellError.Name, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void NameReferringToItselfReturnsCircularError()
    {
        var workbook = CreateWorkbook();
        workbook.DefinedNames["Loop"] = "Loop+1";
        var sheet = workbook.Sheets[0];

        sheet.Cells["C1"].Formula = "=Loop";

        Assert.Equal(CellError.Circular, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void DefinedNamesRoundTripThroughXlsx()
    {
        var workbook = CreateWorkbook();
        workbook.Sheets[0].Cells["C1"].Formula = "=SUMIFS(PLANhours,PLANunit,\"H\")";

        using var ms = new MemoryStream();
        workbook.SaveToStream(ms);
        ms.Position = 0;

        var loaded = Workbook.LoadFromStream(ms);

        Assert.Equal("'Plan input'!$A$1:$A$3", loaded.DefinedNames["PLANhours"]);
        Assert.Equal(4d, loaded.Sheets[0].Cells["C1"].Value);
    }

    [Fact]
    public void CrossSheetFormulasEvaluateAfterLoadingFromXlsx()
    {
        var workbook = new Workbook();
        var first = workbook.AddSheet("First", 5, 5);
        var second = workbook.AddSheet("Second", 5, 5);
        second.Cells["A1"].Value = 7;
        first.Cells["A1"].Formula = "=Second!A1*2";

        using var ms = new MemoryStream();
        workbook.SaveToStream(ms);
        ms.Position = 0;

        var loaded = Workbook.LoadFromStream(ms);

        Assert.Equal(14d, loaded.Sheets[0].Cells["A1"].Value);
    }
}
