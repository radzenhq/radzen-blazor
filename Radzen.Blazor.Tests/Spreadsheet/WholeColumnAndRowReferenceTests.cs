using System.IO;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

public class WholeColumnAndRowReferenceTests
{
    private static Worksheet CreateData(Workbook workbook, string name = "Data")
    {
        var data = workbook.AddSheet(name, 10, 5);
        data.Cells["A1"].Value = 10;
        data.Cells["A2"].Value = 20;
        data.Cells["A4"].Value = 30;
        data.Cells["B4"].Value = 40;
        return data;
    }

    [Theory]
    [InlineData("=SUM(A:A)", 60d)]
    [InlineData("=SUM(4:4)", 70d)]
    [InlineData("=SUM($A:$A)", 60d)]
    [InlineData("=SUM($4:$4)", 70d)]
    [InlineData("=SUM(A:B)", 100d)]
    [InlineData("=SUM(B:A)", 100d)]
    [InlineData("=SUM(4:5)", 70d)]
    [InlineData("=SUM(A1:A10)", 60d)]
    [InlineData("=SUM(A4:E4)", 70d)]
    public void SameSheetReference_EvaluatesLikeExcel(string formula, double expected)
    {
        var data = CreateData(new Workbook());

        data.Cells["E10"].Formula = formula;

        Assert.Equal(expected, data.Cells["E10"].Value);
    }

    [Theory]
    [InlineData("=SUM(Data!A:A)", 60d)]
    [InlineData("=SUM(Data!4:4)", 70d)]
    [InlineData("=SUM(Data!$A:$B)", 100d)]
    [InlineData("=SUM(Data!$4:$5)", 70d)]
    public void CrossSheetReference_Evaluates(string formula, double expected)
    {
        var workbook = new Workbook();
        CreateData(workbook);
        var report = workbook.AddSheet("Report", 10, 5);

        report.Cells["A1"].Formula = formula;

        Assert.Equal(expected, report.Cells["A1"].Value);
    }

    [Fact]
    public void QuotedSheetReference_Evaluates()
    {
        var workbook = new Workbook();
        CreateData(workbook, "My Data");
        var report = workbook.AddSheet("Report", 10, 5);

        report.Cells["A1"].Formula = "=SUM('My Data'!A:A)+SUM('My Data'!4:4)";

        Assert.Equal(130d, report.Cells["A1"].Value);
    }

    [Theory]
    [InlineData("=ROWS(A:A)", 1048576d)]
    [InlineData("=COLUMNS(A:C)", 3d)]
    [InlineData("=ROWS(2:4)", 3d)]
    [InlineData("=COLUMNS(4:4)", 16384d)]
    [InlineData("=COUNTBLANK(A:A)", 1048573d)]
    [InlineData("=COUNTA(A:A)", 3d)]
    [InlineData("=INDEX(A:A,2)", 20d)]
    [InlineData("=INDEX(A:A,1000000)", 0d)]
    [InlineData("=INDEX(4:4,1,2)", 40d)]
    [InlineData("=INDEX(4:4,1,16000)", 0d)]
    [InlineData("=VLOOKUP(30,A:B,2,FALSE)", 40d)]
    [InlineData("=MATCH(30,A:A,0)", 4d)]
    [InlineData("=SUMIF(A:A,\">15\",B:B)", 40d)]
    public void FunctionsSeeTheWholeColumnOrRow(string formula, double expected)
    {
        var data = CreateData(new Workbook());

        data.Cells["E10"].Formula = formula;

        Assert.Equal(expected, data.Cells["E10"].Value);
    }

    [Fact]
    public void ImplicitIntersection_ReturnsCellInFormulaRow()
    {
        var data = CreateData(new Workbook());

        data.Cells["C2"].Formula = "=A:A";
        data.Cells["C4"].Formula = "=4:4";

        Assert.Equal(20d, data.Cells["C2"].Value);
        Assert.Equal(CellError.Circular, data.Cells["C4"].Value);
    }

    [Fact]
    public void ImplicitIntersection_BeyondReferencedSheetRows_IsEmpty()
    {
        var workbook = new Workbook();
        CreateData(workbook);
        var report = workbook.AddSheet("Report", 50, 5);

        report.Cells["A40"].Formula = "=Data!A:A";

        Assert.Equal(0d, report.Cells["A40"].Value);
    }

    [Fact]
    public void ColumnReferenceInsideItsOwnColumn_IsCircular()
    {
        var data = CreateData(new Workbook());

        data.Cells["A10"].Formula = "=SUM(A:A)";

        Assert.Equal(CellError.Circular, data.Cells["A10"].Value);
    }

    [Fact]
    public void EnteringWholeColumnFormula_DoesNotMaterializeCells()
    {
        var sheet = new Worksheet(1000, 10);
        sheet.Cells["A1"].Value = 1;

        sheet.Cells["B1"].Formula = "=SUM(A:A)+SUM(5:5)";

        Assert.Equal(2, sheet.Cells.PopulatedCount);
        Assert.Equal(1d, sheet.Cells["B1"].Value);
    }

    [Fact]
    public void ValueEnteredIntoEmptyCellOfReferencedColumn_Recalculates()
    {
        var data = CreateData(new Workbook());
        data.Cells["E10"].Formula = "=SUM(A:A)";

        data.Cells["A7"].Value = 5;

        Assert.Equal(65d, data.Cells["E10"].Value);
    }

    [Fact]
    public void ValueEnteredIntoEmptyCellOfReferencedRow_Recalculates()
    {
        var data = CreateData(new Workbook());
        data.Cells["E10"].Formula = "=SUM(4:4)";

        data.Cells["D4"].Value = 5;

        Assert.Equal(75d, data.Cells["E10"].Value);
    }

    [Fact]
    public void ValueEnteredOnReferencedSheet_RecalculatesCrossSheetColumnReference()
    {
        var workbook = new Workbook();
        var data = CreateData(workbook);
        var report = workbook.AddSheet("Report", 10, 5);
        report.Cells["A1"].Formula = "=SUM(Data!A:A)";

        data.Cells["A9"].Value = 1;

        Assert.Equal(61d, report.Cells["A1"].Value);
    }

    [Fact]
    public void ValueEnteredOutsideReferencedColumns_DoesNotRecalculate()
    {
        var data = CreateData(new Workbook());
        data.Cells["E10"].Formula = "=SUM(A:B)";
        var evaluations = 0;
        data.Cells["E10"].Changed += _ => evaluations++;

        data.Cells["C1"].Value = 5;

        Assert.Equal(0, evaluations);
    }

    [Fact]
    public void ReplacingWholeColumnFormula_StopsTrackingOldColumn()
    {
        var data = CreateData(new Workbook());
        data.Cells["E10"].Formula = "=SUM(A:A)";
        data.Cells["E10"].Formula = "=SUM(B:B)";
        var evaluations = 0;
        data.Cells["E10"].Changed += _ => evaluations++;

        data.Cells["A9"].Value = 5;

        Assert.Equal(0, evaluations);
        Assert.Equal(40d, data.Cells["E10"].Value);
    }

    [Fact]
    public void InsertingColumnBeforeReferencedColumn_ShiftsReference()
    {
        var data = CreateData(new Workbook());
        data.Cells["E10"].Formula = "=SUM(A:A)";

        data.InsertColumn(0);

        Assert.Equal("=SUM(B:B)", data.Cells["F10"].Formula);
        Assert.Equal(60d, data.Cells["F10"].Value);
    }

    [Fact]
    public void InsertingRow_KeepsColumnReference()
    {
        var data = CreateData(new Workbook());
        data.Cells["E10"].Formula = "=SUM(A:A)";

        data.InsertRow(0);

        Assert.Equal("=SUM(A:A)", data.Cells["E11"].Formula);
        Assert.Equal(60d, data.Cells["E11"].Value);
    }

    [Fact]
    public void InsertingRowAboveReferencedRow_ShiftsReference()
    {
        var data = CreateData(new Workbook());
        data.Cells["E10"].Formula = "=SUM(4:4)";

        data.InsertRow(0);

        Assert.Equal("=SUM(5:5)", data.Cells["E11"].Formula);
        Assert.Equal(70d, data.Cells["E11"].Value);
    }

    [Fact]
    public void InsertingRowOnReferencedSheet_ShiftsCrossSheetRowReference()
    {
        var workbook = new Workbook();
        var data = CreateData(workbook);
        var report = workbook.AddSheet("Report", 10, 5);
        report.Cells["A1"].Formula = "=SUM(Data!4:4)";

        data.InsertRow(0);

        Assert.Equal("=SUM(Data!5:5)", report.Cells["A1"].Formula);
        Assert.Equal(70d, report.Cells["A1"].Value);
    }

    [Fact]
    public void DeletingReferencedColumn_MakesReferenceRefError()
    {
        var data = CreateData(new Workbook());
        data.Cells["E10"].Formula = "=SUM(A:A)";

        data.DeleteColumn(0);

        Assert.Equal("=SUM(#REF!)", data.Cells["D10"].Formula);
        Assert.Equal(CellError.Ref, data.Cells["D10"].Value);
    }

    [Fact]
    public void DeletingReferencedRow_MakesReferenceRefError()
    {
        var data = CreateData(new Workbook());
        data.Cells["E10"].Formula = "=SUM(4:4)";

        data.DeleteRow(3);

        Assert.Equal("=SUM(#REF!)", data.Cells["E9"].Formula);
        Assert.Equal(CellError.Ref, data.Cells["E9"].Value);
    }

    [Fact]
    public void DeletingRow_KeepsColumnReference()
    {
        var data = CreateData(new Workbook());
        data.Cells["E10"].Formula = "=SUM(A:A)";

        data.DeleteRow(0);

        Assert.Equal("=SUM(A:A)", data.Cells["E9"].Formula);
        Assert.Equal(50d, data.Cells["E9"].Value);
    }

    [Fact]
    public void DeletingColumn_KeepsRowReference()
    {
        var data = CreateData(new Workbook());
        data.Cells["E10"].Formula = "=SUM(4:4)";

        data.DeleteColumn(0);

        Assert.Equal("=SUM(4:4)", data.Cells["D10"].Formula);
        Assert.Equal(40d, data.Cells["D10"].Value);
    }

    [Fact]
    public void DeletingUnreferencedColumn_KeepsColumnReference()
    {
        var data = CreateData(new Workbook());
        data.Cells["E10"].Formula = "=SUM(A:A)";

        data.DeleteColumn(2);

        Assert.Equal("=SUM(A:A)", data.Cells["D10"].Formula);
        Assert.Equal(60d, data.Cells["D10"].Value);
    }

    [Fact]
    public void Xlsx_RoundTripsWholeColumnAndRowFormulas()
    {
        var workbook = new Workbook();
        var data = CreateData(workbook);
        data.Cells["E10"].Formula = "=SUM(A:A)+SUM($4:$4)";

        using var stream = new MemoryStream();
        workbook.SaveToStream(stream);
        stream.Position = 0;
        var loaded = Workbook.LoadFromStream(stream).Sheets[0];

        Assert.Equal("=SUM(A:A)+SUM($4:$4)", loaded.Cells["E10"].Formula);
        Assert.Equal(130d, loaded.Cells["E10"].Value);
    }
}
