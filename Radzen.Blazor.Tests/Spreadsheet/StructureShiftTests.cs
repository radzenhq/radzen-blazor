using System.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

public class StructureShiftTests
{
    private static Worksheet NewSheet(int rows = 30, int cols = 12)
    {
        var wb = new Workbook();
        var ws = wb.AddSheet("Sheet1", rows, cols);

        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                ws.Cells[r, c].Value = (double)(r * 100 + c);
            }
        }

        return ws;
    }

    [Fact]
    public void InsertRow_ShiftsSheetFilterRange()
    {
        var ws = NewSheet();
        ws.AddFilter(new SheetFilter(new GreaterThanCriterion { Column = 2, Value = 500 }, RangeRef.Parse("C5:C10")));

        ws.InsertRow(2);
        Assert.Equal(RangeRef.Parse("C6:C11"), ws.Filters[0].Range);

        ws.InsertRow(8);
        Assert.Equal(RangeRef.Parse("C6:C12"), ws.Filters[0].Range);

        ws.DeleteRow(0);
        Assert.Equal(RangeRef.Parse("C5:C11"), ws.Filters[0].Range);
    }

    [Fact]
    public void InsertColumn_ShiftsSheetFilterRangeAndCriterionColumn()
    {
        var ws = NewSheet();
        var criterion = new AndCriterion
        {
            Criteria = [new GreaterThanCriterion { Column = 2, Value = 1 }, new TopFilterCriterion { Column = 2, Count = 3 }]
        };
        ws.AddFilter(new SheetFilter(criterion, RangeRef.Parse("C5:C10")));

        ws.InsertColumn(1);

        Assert.Equal(RangeRef.Parse("D5:D10"), ws.Filters[0].Range);
        Assert.Equal(3, ((FilterCriterionLeaf)criterion.Criteria[0]).Column);
        Assert.Equal(3, ((TopFilterCriterion)criterion.Criteria[1]).Column);

        ws.DeleteColumn(0);

        Assert.Equal(RangeRef.Parse("C5:C10"), ws.Filters[0].Range);
        Assert.Equal(2, ((FilterCriterionLeaf)criterion.Criteria[0]).Column);
    }

    [Fact]
    public void DeleteColumn_OfFilteredColumn_RemovesFilter()
    {
        var ws = NewSheet();
        ws.AddFilter(new SheetFilter(new GreaterThanCriterion { Column = 2, Value = 1 }, RangeRef.Parse("C5:C10")));

        ws.DeleteColumn(2);

        Assert.Empty(ws.Filters);
    }

    [Fact]
    public void InsertRow_ShiftsConditionalFormatRanges()
    {
        var ws = NewSheet();
        var rule = new GreaterThanRule { Value = 1, Format = new Format { Bold = true } };
        ws.ConditionalFormats.Add(RangeRef.Parse("B5:D8"), rule);
        var chromeChanged = 0;
        ws.ChromeChanged += () => chromeChanged++;

        ws.InsertRow(6);

        Assert.Equal([RangeRef.Parse("B5:D9")], ws.ConditionalFormats.Ranges);
        Assert.Same(rule, ws.ConditionalFormats.GetRules(RangeRef.Parse("B5:D9")).Single());
        Assert.Equal(1, chromeChanged);

        ws.DeleteRow(2);
        Assert.Equal([RangeRef.Parse("B4:D8")], ws.ConditionalFormats.Ranges);

        ws.InsertColumn(2);
        Assert.Equal([RangeRef.Parse("B4:E8")], ws.ConditionalFormats.Ranges);

        ws.DeleteColumn(1);
        Assert.Equal([RangeRef.Parse("B4:D8")], ws.ConditionalFormats.Ranges);
    }

    [Fact]
    public void DeleteRow_OfSingleRowConditionalFormat_RemovesIt()
    {
        var ws = NewSheet();
        ws.ConditionalFormats.Add(RangeRef.Parse("B5:D5"), new GreaterThanRule { Value = 1 });

        ws.DeleteRow(4);

        Assert.Empty(ws.ConditionalFormats.Ranges);
    }

    [Fact]
    public void InsertRow_ShiftsValidationRangesAndFormulas()
    {
        var ws = NewSheet();
        var rule = new DataValidationRule { Type = DataValidationType.List, Formula1 = "$A$10:$A$12" };
        ws.Validation.Add(RangeRef.Parse("C3:C6"), rule);

        ws.InsertRow(4);

        Assert.Equal([RangeRef.Parse("C3:C7")], ws.Validation.Ranges);
        Assert.Same(rule, ws.Validation.GetValidators(RangeRef.Parse("C3:C7")).Single());
        Assert.Equal("$A$11:$A$13", rule.Formula1);

        ws.DeleteRow(11);
        Assert.Equal("$A$11:$A$12", rule.Formula1);

        ws.InsertColumn(0);
        Assert.Equal([RangeRef.Parse("D3:D7")], ws.Validation.Ranges);
        Assert.Equal("$B$11:$B$12", rule.Formula1);
    }

    [Fact]
    public void Validation_ConstantFormulasAreLeftAlone()
    {
        var ws = NewSheet();
        var rule = new DataValidationRule { Type = DataValidationType.WholeNumber, Operator = DataValidationOperator.Between, Formula1 = "1", Formula2 = "10" };
        ws.Validation.Add(RangeRef.Parse("C3:C6"), rule);

        ws.InsertRow(0);

        Assert.Equal("1", rule.Formula1);
        Assert.Equal("10", rule.Formula2);
    }

    [Fact]
    public void InsertRow_ShiftsImageAndChartAnchors()
    {
        var ws = NewSheet();
        var image = new SheetImage
        {
            AnchorMode = DrawingAnchorMode.TwoCellAnchor,
            From = new CellAnchor { Row = 5, Column = 2, RowOffset = 3 },
            To = new CellAnchor { Row = 9, Column = 5 },
            Data = [1, 2, 3],
        };
        ws.AddImage(image);
        var chart = new SheetChart { From = new CellAnchor { Row = 2, Column = 1 }, To = new CellAnchor { Row = 12, Column = 4 } };
        ws.AddChart(chart);

        ws.InsertRow(5, 2);

        Assert.Equal(7, image.From.Row);
        Assert.Equal(3, image.From.RowOffset);
        Assert.Equal(11, image.To!.Row);
        Assert.Equal(2, chart.From.Row);
        Assert.Equal(14, chart.To!.Row);

        ws.DeleteRow(0);
        Assert.Equal(6, image.From.Row);
        Assert.Equal(1, chart.From.Row);

        ws.DeleteRow(6);
        Assert.Equal(6, image.From.Row);
        Assert.Equal(9, image.To.Row);

        ws.InsertColumn(2, 3);
        Assert.Equal(5, image.From.Column);
        Assert.Equal(8, image.To.Column);
        Assert.Equal(1, chart.From.Column);
        Assert.Equal(7, chart.To.Column);

        ws.DeleteColumn(0);
        Assert.Equal(4, image.From.Column);
        Assert.Equal(0, chart.From.Column);
    }

    [Fact]
    public void InsertRow_ShiftsChartSeriesReferences()
    {
        var ws = NewSheet();
        var chart = new SheetChart { From = new CellAnchor { Row = 0, Column = 6 } };
        chart.Series.Add(new ChartSeries { Name = "Sheet1!$B$1", Categories = "Sheet1!$A$2:$A$6", Values = "Sheet1!$B$2:$B$6" });
        ws.AddChart(chart);

        ws.InsertRow(3);

        Assert.Equal("Sheet1!$B$1", chart.Series[0].Name);
        Assert.Equal("Sheet1!$A$2:$A$7", chart.Series[0].Categories);
        Assert.Equal("Sheet1!$B$2:$B$7", chart.Series[0].Values);

        ws.DeleteRow(6);

        Assert.Equal("Sheet1!$A$2:$A$6", chart.Series[0].Categories);

        ws.DeleteRow(0);

        Assert.Equal("Sheet1!$A$1:$A$5", chart.Series[0].Categories);
        Assert.Equal("Sheet1!$B$1:$B$5", chart.Series[0].Values);

        ws.InsertColumn(1);

        Assert.Equal("Sheet1!$C$1", chart.Series[0].Name);
        Assert.Equal("Sheet1!$A$1:$A$5", chart.Series[0].Categories);
        Assert.Equal("Sheet1!$C$1:$C$5", chart.Series[0].Values);
    }

    [Fact]
    public void InsertRow_ShiftsChartSeriesReferencesFromOtherSheets()
    {
        var ws = NewSheet();
        var other = ws.Workbook.AddSheet("Report", 10, 10);
        var chart = new SheetChart { From = new CellAnchor { Row = 0, Column = 0 } };
        chart.Series.Add(new ChartSeries { Values = "Sheet1!$B$2:$B$6" });
        other.AddChart(chart);
        var local = new SheetChart { From = new CellAnchor { Row = 0, Column = 0 } };
        local.Series.Add(new ChartSeries { Values = "$B$2:$B$6" });
        other.AddChart(local);

        ws.InsertRow(0);

        Assert.Equal("Sheet1!$B$3:$B$7", chart.Series[0].Values);
        Assert.Equal("$B$2:$B$6", local.Series[0].Values);
    }

    [Fact]
    public void DeleteRowsCommand_Undo_RestoresFullyDeletedTable()
    {
        var ws = NewSheet();
        var table = ws.AddTable("T", RangeRef.Parse("B5:D7"));
        table.Columns[1].Name = "Amount";
        table.Columns[1].TotalsCalculation = TotalsCalculation.Sum;

        var command = new DeleteRowsCommand(ws, 4, 6);
        command.Execute();

        Assert.Empty(ws.Tables);

        command.Unexecute();

        Assert.Same(table, ws.Tables.Single());
        Assert.Equal(RangeRef.Parse("B5:D7"), table.Range);
        Assert.Equal(RangeRef.Parse("B5:D7"), table.Filter.Range);
        Assert.Equal("Amount", table.Columns[1].Name);
        Assert.Equal(TotalsCalculation.Sum, table.Columns[1].TotalsCalculation);
    }

    [Fact]
    public void DeleteRowsCommand_Undo_RestoresTableTrimmedAtItsEnd()
    {
        var ws = NewSheet();
        var table = ws.AddTable("T", RangeRef.Parse("B5:D9"), hasHeaders: true, totalsRowShown: true);

        var command = new DeleteRowsCommand(ws, 7, 8);
        command.Execute();

        Assert.Equal(RangeRef.Parse("B5:D7"), table.Range);

        command.Unexecute();

        Assert.Equal(RangeRef.Parse("B5:D9"), table.Range);
        Assert.True(table.ShowTotals);
    }

    [Fact]
    public void DeleteRowsCommand_Undo_RestoresTableHeaderRow()
    {
        var ws = NewSheet();
        var table = ws.AddTable("T", RangeRef.Parse("B5:D9"));

        var command = new DeleteRowsCommand(ws, 4, 4);
        command.Execute();

        Assert.Equal(RangeRef.Parse("B5:D8"), table.Range);

        command.Unexecute();

        Assert.Equal(RangeRef.Parse("B5:D9"), table.Range);
    }

    [Fact]
    public void DeleteColumnsCommand_Undo_RestoresTableColumns()
    {
        var ws = NewSheet();
        var table = ws.AddTable("T", RangeRef.Parse("B5:E9"));
        table.Columns[2].Name = "Amount";

        var command = new DeleteColumnsCommand(ws, 3, 4);
        command.Execute();

        Assert.Equal(RangeRef.Parse("B5:C9"), table.Range);
        Assert.Equal(2, table.Columns.Count);

        command.Unexecute();

        Assert.Equal(RangeRef.Parse("B5:E9"), table.Range);
        Assert.Equal(4, table.Columns.Count);
        Assert.Equal([0, 1, 2, 3], table.Columns.Select(c => c.Index));
        Assert.Equal("Amount", table.Columns[2].Name);
    }
}
