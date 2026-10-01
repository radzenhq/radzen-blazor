using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class CrossSheetRecalculationTests
{
    [Fact]
    public void ChangingReferencedCellOnAnotherSheet_RecalculatesFormula()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet("Sheet1", 10, 10);
        var sheet2 = workbook.AddSheet("Sheet2", 10, 10);
        sheet2.Cells["A1"].Value = 1;
        sheet1.Cells["A1"].Formula = "=Sheet2!A1*10";

        sheet2.Cells["A1"].Value = 5;

        Assert.Equal(50d, sheet1.Cells["A1"].Value);
    }

    [Fact]
    public void ChangingCellInsideCrossSheetRange_RecalculatesFormula()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet("Sheet1", 10, 10);
        var sheet2 = workbook.AddSheet("Sheet2", 10, 10);
        sheet1.Cells["A1"].Formula = "=SUM(Sheet2!A1:A3)";

        sheet2.Cells["A1"].Value = 5;
        sheet2.Cells["A3"].Value = 7;

        Assert.Equal(12d, sheet1.Cells["A1"].Value);
    }

    [Fact]
    public void SheetNameInReference_IsCaseInsensitiveForRecalculation()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet("Sheet1", 10, 10);
        var sheet2 = workbook.AddSheet("Sheet2", 10, 10);
        sheet1.Cells["A1"].Formula = "=sheet2!A1";

        sheet2.Cells["A1"].Value = 3;

        Assert.Equal(3d, sheet1.Cells["A1"].Value);
    }

    [Fact]
    public void ChainAcrossThreeSheets_RecalculatesEachFormulaOnce()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet("Sheet1", 10, 10);
        var sheet2 = workbook.AddSheet("Sheet2", 10, 10);
        var sheet3 = workbook.AddSheet("Sheet3", 10, 10);
        sheet3.Cells["A1"].Value = 1;
        sheet2.Cells["A1"].Formula = "=Sheet3!A1*2";
        sheet1.Cells["A1"].Formula = "=Sheet2!A1+Sheet3!A1";

        var sheet1Evaluations = 0;
        var sheet2Evaluations = 0;
        sheet1.Cells["A1"].Changed += _ => sheet1Evaluations++;
        sheet2.Cells["A1"].Changed += _ => sheet2Evaluations++;

        sheet3.Cells["A1"].Value = 5;

        Assert.Equal(10d, sheet2.Cells["A1"].Value);
        Assert.Equal(15d, sheet1.Cells["A1"].Value);
        Assert.Equal(1, sheet1Evaluations);
        Assert.Equal(1, sheet2Evaluations);
    }

    [Fact]
    public void CycleAcrossSheets_IsCircular()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet("Sheet1", 10, 10);
        var sheet2 = workbook.AddSheet("Sheet2", 10, 10);
        sheet1.Cells["A1"].Formula = "=Sheet2!A1+1";

        sheet2.Cells["A1"].Formula = "=Sheet1!A1+1";

        Assert.Equal(CellError.Circular, sheet1.Cells["A1"].Value);
        Assert.Equal(CellError.Circular, sheet2.Cells["A1"].Value);
    }

    [Fact]
    public void BatchOnReferencedSheet_DefersDependentFormulaOnAnotherSheetUntilEndUpdate()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet("Sheet1", 10, 10);
        var sheet2 = workbook.AddSheet("Sheet2", 10, 10);
        sheet2.Cells["A1"].Value = 1;
        sheet1.Cells["A1"].Formula = "=Sheet2!A1";

        sheet2.BeginUpdate();
        sheet2.Cells["A1"].Value = 2;
        Assert.Equal(1d, sheet1.Cells["A1"].Value);
        sheet2.EndUpdate();

        Assert.Equal(2d, sheet1.Cells["A1"].Value);
    }

    [Fact]
    public void ChangeOnAnotherSheetDuringBatch_IsRecalculatedAtEndUpdate()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet("Sheet1", 10, 10);
        var sheet2 = workbook.AddSheet("Sheet2", 10, 10);
        sheet2.Cells["A1"].Value = 1;
        sheet1.Cells["A1"].Formula = "=Sheet2!A1";

        sheet1.BeginUpdate();
        sheet2.Cells["A1"].Value = 2;
        Assert.Equal(1d, sheet1.Cells["A1"].Value);
        sheet1.EndUpdate();

        Assert.Equal(2d, sheet1.Cells["A1"].Value);
    }

    [Fact]
    public void RemovingReferencedSheet_MakesFormulaRefError()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet("Sheet1", 10, 10);
        var data = workbook.AddSheet("Data", 10, 10);
        data.Cells["A1"].Value = 3;
        sheet1.Cells["A1"].Formula = "=Data!A1";

        workbook.RemoveSheet(data);

        Assert.Equal(CellError.Ref, sheet1.Cells["A1"].Value);
    }

    [Fact]
    public void AddingSheetNamedByFormula_ResolvesAndTracksIt()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet("Sheet1", 10, 10);
        sheet1.Cells["A1"].Formula = "=Data!A1";
        Assert.Equal(CellError.Ref, sheet1.Cells["A1"].Value);

        var data = workbook.AddSheet("Data", 10, 10);
        data.Cells["A1"].Value = 7;

        Assert.Equal(7d, sheet1.Cells["A1"].Value);
    }

    [Fact]
    public void RenamingSheetToNameUsedByFormula_ResolvesAndTracksIt()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet("Sheet1", 10, 10);
        var sheet2 = workbook.AddSheet("Sheet2", 10, 10);
        sheet1.Cells["A1"].Formula = "=Data!A1";

        sheet2.Name = "Data";
        sheet2.Cells["A1"].Value = 4;

        Assert.Equal(4d, sheet1.Cells["A1"].Value);
    }

    [Fact]
    public void RenamingReferencedSheet_MakesFormulaRefError()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet("Sheet1", 10, 10);
        var data = workbook.AddSheet("Data", 10, 10);
        data.Cells["A1"].Value = 3;
        sheet1.Cells["A1"].Formula = "=Data!A1";

        data.Name = "Other";

        Assert.Equal(CellError.Ref, sheet1.Cells["A1"].Value);
    }

    [Fact]
    public void AddingSheetWithFormulas_TracksReferencesToExistingSheets()
    {
        var workbook = new Workbook();
        var data = workbook.AddSheet("Data", 10, 10);
        data.Cells["A1"].Value = 2;
        var report = new Worksheet(10, 10) { Name = "Report" };
        report.Cells["A1"].Formula = "=Data!A1*3";

        workbook.AddSheet(report);
        Assert.Equal(6d, report.Cells["A1"].Value);

        data.Cells["A1"].Value = 5;

        Assert.Equal(15d, report.Cells["A1"].Value);
    }

    [Fact]
    public void InsertingRowOnReferencedSheet_ShiftsCrossSheetReference()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet("Sheet1", 10, 10);
        var sheet2 = workbook.AddSheet("Sheet2", 10, 10);
        sheet2.Cells["A5"].Value = 9;
        sheet1.Cells["B1"].Formula = "=Sheet2!A5+A5";

        sheet2.InsertRow(0);

        Assert.Equal("=Sheet2!A6+A5", sheet1.Cells["B1"].Formula);
        Assert.Equal(9d, sheet1.Cells["B1"].Value);
    }

    [Fact]
    public void InsertingRowOnFormulaSheet_DoesNotShiftReferencesToAnotherSheet()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet("Sheet1", 10, 10);
        var sheet2 = workbook.AddSheet("Sheet2", 10, 10);
        sheet2.Cells["A5"].Value = 9;
        sheet1.Cells["B1"].Formula = "=Sheet2!A5+A5";

        sheet1.InsertRow(0);

        Assert.Equal("=Sheet2!A5+A6", sheet1.Cells["B2"].Formula);
        Assert.Equal(9d, sheet1.Cells["B2"].Value);
    }

    [Fact]
    public void DeletingReferencedRowOnAnotherSheet_MakesCrossSheetReferenceRefError()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet("Sheet1", 10, 10);
        var sheet2 = workbook.AddSheet("Sheet2", 10, 10);
        sheet2.Cells["A1"].Value = 9;
        sheet1.Cells["B1"].Formula = "=Sheet2!A1+A1";

        sheet2.DeleteRow(0);

        Assert.Equal("=#REF!+A1", sheet1.Cells["B1"].Formula);
        Assert.Equal(CellError.Ref, sheet1.Cells["B1"].Value);
    }

    [Fact]
    public void ClearingFormula_StopsRecalculatingItsCell()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet("Sheet1", 10, 10);
        var sheet2 = workbook.AddSheet("Sheet2", 10, 10);
        sheet1.Cells["A1"].Formula = "=Sheet2!A1";
        sheet1.Cells["A1"].Formula = null;
        sheet1.Cells["A1"].Value = 42;

        sheet2.Cells["A1"].Value = 1;

        Assert.Equal(42d, sheet1.Cells["A1"].Value);
    }
}
