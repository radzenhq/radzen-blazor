using System.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class DeleteFormulaReferenceTests
{
    private static Worksheet CreateReport()
    {
        var sheet = new Worksheet(10, 3);

        for (var row = 0; row < 5; row++)
        {
            sheet.Cells[row, 0].Value = (row + 1) * 10d;
            sheet.Cells[row, 1].Formula = $"=A{row + 1}*2";
        }

        sheet.Cells["A7"].Formula = "=SUM(A1:A5)";
        sheet.Cells["B7"].Formula = "=A5";

        return sheet;
    }

    private static void Fill(Worksheet sheet, int rows)
    {
        for (var row = 0; row < rows; row++)
        {
            sheet.Cells[row, 0].Value = row + 1d;
        }
    }

    private static void DeleteRows(Worksheet sheet, int start, int end) =>
        Assert.True(new SheetView(sheet).Commands.Execute(new DeleteRowsCommand(sheet, start, end)));

    [Fact]
    public void DeleteRow_MatchesExcelForTheReport()
    {
        var sheet = CreateReport();

        DeleteRows(sheet, 1, 1);

        Assert.Equal("=A2*2", sheet.Cells["B2"].Formula);
        Assert.Equal(60d, sheet.Cells["B2"].Value);
        Assert.Equal("=A3*2", sheet.Cells["B3"].Formula);
        Assert.Equal(80d, sheet.Cells["B3"].Value);
        Assert.Equal("=A4*2", sheet.Cells["B4"].Formula);
        Assert.Equal(100d, sheet.Cells["B4"].Value);
        Assert.Equal("=SUM(A1:A4)", sheet.Cells["A6"].Formula);
        Assert.Equal(130d, sheet.Cells["A6"].Value);
        Assert.Equal("=A4", sheet.Cells["B6"].Formula);
        Assert.Equal(50d, sheet.Cells["B6"].Value);
    }

    [Fact]
    public void DeleteRows_MatchesExcelForTheReport()
    {
        var sheet = CreateReport();

        DeleteRows(sheet, 1, 3);

        Assert.Equal("=A2*2", sheet.Cells["B2"].Formula);
        Assert.Equal(100d, sheet.Cells["B2"].Value);
        Assert.Equal("=SUM(A1:A2)", sheet.Cells["A4"].Formula);
        Assert.Equal(60d, sheet.Cells["A4"].Value);
        Assert.Equal("=A2", sheet.Cells["B4"].Formula);
        Assert.Equal(50d, sheet.Cells["B4"].Value);
    }

    [Theory]
    [InlineData("=SUM(A2:A3)", 1, 2, "=SUM(#REF!)")]
    [InlineData("=SUM(A3:A4)", 1, 2, "=SUM(A2:A2)")]
    [InlineData("=SUM(A1:A3)", 1, 2, "=SUM(A1:A1)")]
    [InlineData("=SUM(A2:A5)", 1, 1, "=SUM(A2:A4)")]
    [InlineData("=SUM(A5:A2)", 1, 1, "=SUM(A2:A4)")]
    [InlineData("=$A$5+A$4+$A3", 1, 1, "=$A$4+A$3+$A2")]
    [InlineData("=A1+A3", 0, 0, "=#REF!+A2")]
    [InlineData("=SUM(A:A)", 3, 3, "=SUM(A:A)")]
    [InlineData("=SUM(4:4)", 3, 3, "=SUM(#REF!)")]
    [InlineData("=SUM(5:5)", 3, 3, "=SUM(4:4)")]
    [InlineData("=SUM(3:5)", 3, 3, "=SUM(3:4)")]
    [InlineData("=SUM(A:C)", 3, 3, "=SUM(A:C)")]
    public void DeleteRows_RewritesReferenceLikeExcel(string formula, int start, int end, string expected)
    {
        var sheet = new Worksheet(10, 8);
        Fill(sheet, 6);
        sheet.Cells["H10"].Formula = formula;

        DeleteRows(sheet, start, end);

        Assert.Equal(expected, sheet.Cells[9 - (end - start + 1), 7].Formula);
    }

    [Fact]
    public void DeleteColumn_MatchesExcel()
    {
        var sheet = new Worksheet(5, 8);

        for (var column = 0; column < 5; column++)
        {
            sheet.Cells[0, column].Value = (column + 1) * 10d;
            sheet.Cells[1, column].Formula = $"={ColumnRef.ToString(column)}1*2";
        }

        sheet.Cells["G1"].Formula = "=SUM(A1:E1)";
        sheet.Cells["G2"].Formula = "=E1";
        sheet.Cells["G3"].Formula = "=B1";

        Assert.True(new SheetView(sheet).Commands.Execute(new DeleteColumnsCommand(sheet, 1, 1)));

        Assert.Equal("=SUM(A1:D1)", sheet.Cells["F1"].Formula);
        Assert.Equal(130d, sheet.Cells["F1"].Value);
        Assert.Equal("=B1*2", sheet.Cells["B2"].Formula);
        Assert.Equal(60d, sheet.Cells["B2"].Value);
        Assert.Equal("=D1", sheet.Cells["F2"].Formula);
        Assert.Equal(50d, sheet.Cells["F2"].Value);
        Assert.Equal("=#REF!", sheet.Cells["F3"].Formula);
        Assert.Equal(CellError.Ref, sheet.Cells["F3"].Value);
    }

    [Theory]
    [InlineData("=Sheet1!A5", "=Sheet1!A4")]
    [InlineData("=Sheet1!A2", "=Sheet1!#REF!")]
    [InlineData("=SUM(Sheet1!A1:A5)", "=SUM(Sheet1!A1:A4)")]
    [InlineData("=SUM(Sheet1!A2:A2)", "=SUM(Sheet1!#REF!)")]
    public void DeleteRow_RewritesReferenceFromAnotherSheetLikeExcel(string formula, string expected)
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet("Sheet1", 10, 3);
        var sheet2 = workbook.AddSheet("Sheet2", 10, 3);
        Fill(sheet1, 5);
        sheet2.Cells["A1"].Formula = formula;

        sheet1.DeleteRow(1);

        Assert.Equal(expected, sheet2.Cells["A1"].Formula);
    }

    [Fact]
    public void DeleteRow_LeavesUnqualifiedReferencesOnOtherSheetsAlone()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet("Sheet1", 10, 3);
        var sheet2 = workbook.AddSheet("Sheet2", 10, 3);
        sheet2.Cells["B1"].Formula = "=A5";

        sheet1.DeleteRow(1);

        Assert.Equal("=A5", sheet2.Cells["B1"].Formula);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 3)]
    public void Undo_RestoresEveryFormulaAndValue(int start, int end)
    {
        var sheet = CreateReport();
        var view = new SheetView(sheet);
        var expected = sheet.Cells.GetPopulatedCells().ToDictionary(cell => cell.Address, cell => (cell.Formula, cell.Value));

        Assert.True(view.Commands.Execute(new DeleteRowsCommand(sheet, start, end)));
        view.Commands.Undo();

        Assert.All(expected, entry =>
        {
            Assert.Equal(entry.Value.Formula, sheet.Cells[entry.Key.Row, entry.Key.Column].Formula);
            Assert.Equal(entry.Value.Value, sheet.Cells[entry.Key.Row, entry.Key.Column].Value);
        });
    }

    [Fact]
    public void Undo_LeavesNoDeletedRowBehind()
    {
        var sheet = CreateReport();
        var view = new SheetView(sheet);

        Assert.True(view.Commands.Execute(new DeleteRowsCommand(sheet, 1, 1)));
        view.Commands.Undo();
        sheet.Cells["C1"].Formula = "=A2";

        Assert.Equal(20d, sheet.Cells["C1"].Value);
    }

    [Fact]
    public void Redo_RepeatsTheDelete()
    {
        var sheet = CreateReport();
        var view = new SheetView(sheet);

        Assert.True(view.Commands.Execute(new DeleteRowsCommand(sheet, 1, 1)));
        view.Commands.Undo();
        view.Commands.Redo();

        Assert.Equal("=SUM(A1:A4)", sheet.Cells["A6"].Formula);
        Assert.Equal(130d, sheet.Cells["A6"].Value);
    }

    [Fact]
    public void Undo_RestoresFormulasOnOtherSheets()
    {
        var workbook = new Workbook();
        var sheet1 = workbook.AddSheet("Sheet1", 10, 3);
        var sheet2 = workbook.AddSheet("Sheet2", 10, 3);
        Fill(sheet1, 5);
        sheet2.Cells["A1"].Formula = "=SUM(Sheet1!A1:A2)";
        sheet2.Cells["A2"].Formula = "=Sheet1!A2";
        var view = new SheetView(sheet1);

        Assert.True(view.Commands.Execute(new DeleteRowsCommand(sheet1, 1, 1)));
        view.Commands.Undo();

        Assert.Equal("=SUM(Sheet1!A1:A2)", sheet2.Cells["A1"].Formula);
        Assert.Equal(3d, sheet2.Cells["A1"].Value);
        Assert.Equal("=Sheet1!A2", sheet2.Cells["A2"].Formula);
        Assert.Equal(2d, sheet2.Cells["A2"].Value);
    }

    [Fact]
    public void DeleteRow_InvalidatesValidationFormulaPointingAtIt()
    {
        var sheet = new Worksheet(10, 3);
        var rule = new DataValidationRule { Type = DataValidationType.List, Formula1 = "=$A$2:$A$2" };
        sheet.Validation.Add(RangeRef.Parse("C1:C2"), rule);

        sheet.DeleteRow(1);

        Assert.Equal("=#REF!", rule.Formula1);
        Assert.Empty(rule.GetListItems(sheet));
    }

    [Fact]
    public void Undo_RestoresValidationFormula()
    {
        var sheet = new Worksheet(10, 3);
        var rule = new DataValidationRule { Type = DataValidationType.List, Formula1 = "=$A$1:$A$2" };
        sheet.Validation.Add(RangeRef.Parse("C1:C2"), rule);
        var view = new SheetView(sheet);

        Assert.True(view.Commands.Execute(new DeleteRowsCommand(sheet, 1, 1)));
        Assert.Equal("=$A$1:$A$1", rule.Formula1);

        view.Commands.Undo();

        Assert.Equal("=$A$1:$A$2", rule.Formula1);
    }

    [Fact]
    public void DeleteRow_InvalidatesChartSeriesPointingAtIt()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 3);
        var series = new ChartSeries { Values = "Sheet1!$A$2:$A$2", Name = "Sheet1!$B$2" };
        var chart = new SheetChart();
        chart.Series.Add(series);
        sheet.AddChart(chart);

        sheet.DeleteRow(1);

        Assert.Equal("Sheet1!#REF!", series.Values);
        Assert.Equal("Sheet1!#REF!", series.Name);
        Assert.Empty(ChartDataResolver.ResolveSeriesData(series, workbook, sheet));
    }

    [Fact]
    public void Undo_RestoresChartSeries()
    {
        var workbook = new Workbook();
        var sheet = workbook.AddSheet("Sheet1", 10, 3);
        var series = new ChartSeries { Values = "Sheet1!$A$1:$A$2" };
        var chart = new SheetChart();
        chart.Series.Add(series);
        sheet.AddChart(chart);
        var view = new SheetView(sheet);

        Assert.True(view.Commands.Execute(new DeleteRowsCommand(sheet, 1, 1)));
        view.Commands.Undo();

        Assert.Equal("Sheet1!$A$1:$A$2", series.Values);
    }
}
