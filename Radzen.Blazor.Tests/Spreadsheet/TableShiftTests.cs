using System.Linq;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

public class TableShiftTests
{
    private static Worksheet NewSheet(int rows = 30, int cols = 12)
    {
        var wb = new Workbook();
        return wb.AddSheet("Sheet1", rows, cols);
    }

    private static Table AddTable(Worksheet ws, string name, string range, bool totals = true)
    {
        var r = RangeRef.Parse(range);
        for (var c = r.Start.Column; c <= r.End.Column; c++)
        {
            ws.Cells[r.Start.Row, c].SetValue($"{name}_H{c}");
        }

        for (var row = r.Start.Row + 1; row <= r.End.Row; row++)
        {
            for (var c = r.Start.Column; c <= r.End.Column; c++)
            {
                ws.Cells[row, c].SetValue($"{name}_R{row}C{c}");
            }
        }

        if (totals)
        {
            ws.Cells[r.End.Row, r.Start.Column].SetValue($"Total {name}");
        }

        return ws.AddTable(name, r, hasHeaders: true, totalsRowShown: totals);
    }

    [Fact]
    public void InsertRow_AboveTable_MovesTableDown()
    {
        var ws = NewSheet();
        var table = AddTable(ws, "T", "B5:D8");
        var chromeChanged = 0;
        ws.ChromeChanged += () => chromeChanged++;

        ws.InsertRow(2, 2);

        Assert.Equal(RangeRef.Parse("B7:D10"), table.Range);
        Assert.Equal(RangeRef.Parse("B7:D10"), table.Filter.Range);
        Assert.Equal(RangeRef.Parse("B7:D7"), table.HeaderRowRange);
        Assert.Equal(RangeRef.Parse("B10:D10"), table.TotalsRowRange);
        Assert.Equal("T_H1", ws.Cells[6, 1].Value);
        Assert.Equal("Total T", ws.Cells[9, 1].Value);
        Assert.Equal(1, chromeChanged);
    }

    [Fact]
    public void InsertRow_AtHeaderRow_MovesTableDown()
    {
        var ws = NewSheet();
        var table = AddTable(ws, "T", "B5:D8");

        ws.InsertRow(4);

        Assert.Equal(RangeRef.Parse("B6:D9"), table.Range);
        Assert.Equal("T_H1", ws.Cells[5, 1].Value);
    }

    [Fact]
    public void InsertRow_InsideTable_GrowsTable()
    {
        var ws = NewSheet();
        var table = AddTable(ws, "T", "B5:D8");

        ws.InsertRow(5);

        Assert.Equal(RangeRef.Parse("B5:D9"), table.Range);
        Assert.Equal(RangeRef.Parse("B5:D5"), table.HeaderRowRange);
        Assert.Equal(RangeRef.Parse("B6:D8"), table.DataBodyRange);
        Assert.Equal(RangeRef.Parse("B9:D9"), table.TotalsRowRange);
        Assert.Null(ws.Cells[5, 1].Value);
        Assert.Equal("Total T", ws.Cells[8, 1].Value);
    }

    [Fact]
    public void InsertRow_AtTotalsRow_GrowsTable()
    {
        var ws = NewSheet();
        var table = AddTable(ws, "T", "B5:D8");

        ws.InsertRow(7);

        Assert.Equal(RangeRef.Parse("B5:D9"), table.Range);
        Assert.Equal("Total T", ws.Cells[8, 1].Value);
    }

    [Fact]
    public void InsertRow_BelowTable_LeavesTableUnchanged()
    {
        var ws = NewSheet();
        var table = AddTable(ws, "T", "B5:D8");

        ws.InsertRow(8);

        Assert.Equal(RangeRef.Parse("B5:D8"), table.Range);
    }

    [Fact]
    public void DeleteRow_AboveTable_MovesTableUp()
    {
        var ws = NewSheet();
        var table = AddTable(ws, "T", "B5:D8");

        ws.DeleteRow(1);

        Assert.Equal(RangeRef.Parse("B4:D7"), table.Range);
        Assert.Equal(RangeRef.Parse("B4:D7"), table.Filter.Range);
        Assert.Equal("T_H1", ws.Cells[3, 1].Value);
        Assert.Equal("Total T", ws.Cells[6, 1].Value);
    }

    [Fact]
    public void DeleteRow_InsideTable_ShrinksTable()
    {
        var ws = NewSheet();
        var table = AddTable(ws, "T", "B5:D8");

        ws.DeleteRow(5);

        Assert.Equal(RangeRef.Parse("B5:D7"), table.Range);
        Assert.Equal(RangeRef.Parse("B6:D6"), table.DataBodyRange);
        Assert.Equal("Total T", ws.Cells[6, 1].Value);
    }

    [Fact]
    public void DeleteRow_BelowTable_LeavesTableUnchanged()
    {
        var ws = NewSheet();
        var table = AddTable(ws, "T", "B5:D8");

        ws.DeleteRow(9);

        Assert.Equal(RangeRef.Parse("B5:D8"), table.Range);
    }

    [Fact]
    public void DeleteRow_LastRowOfTable_RemovesTable()
    {
        var ws = NewSheet();
        var table = AddTable(ws, "T", "B5:D5", totals: false);

        ws.DeleteRow(4);

        Assert.DoesNotContain(table, ws.Tables);
    }

    [Fact]
    public void DeleteRow_LeavingOnlyHeaderRow_DropsTotals()
    {
        var ws = NewSheet();
        var table = AddTable(ws, "T", "B5:D6");

        ws.DeleteRow(5);

        Assert.Equal(RangeRef.Parse("B5:D5"), table.Range);
        Assert.False(table.ShowTotals);
        Assert.True(table.ShowHeaderRow);
    }

    [Fact]
    public void InsertColumn_BeforeTable_MovesTableRight()
    {
        var ws = NewSheet();
        var table = AddTable(ws, "T", "B5:D8");

        ws.InsertColumn(0, 2);

        Assert.Equal(RangeRef.Parse("D5:F8"), table.Range);
        Assert.Equal(RangeRef.Parse("D5:F8"), table.Filter.Range);
        Assert.Equal(3, table.Columns.Count);
        Assert.Equal("T_H1", ws.Cells[4, 3].Value);
    }

    [Fact]
    public void InsertColumn_InsideTable_AddsColumn()
    {
        var ws = NewSheet();
        var table = AddTable(ws, "T", "B5:D8");
        table.Columns[2].Name = "Amount";
        table.Columns[2].TotalsCalculation = TotalsCalculation.Sum;

        ws.InsertColumn(2);

        Assert.Equal(RangeRef.Parse("B5:E8"), table.Range);
        Assert.Equal(4, table.Columns.Count);
        Assert.Equal([0, 1, 2, 3], table.Columns.Select(c => c.Index));
        Assert.Equal("Amount", table.Columns[3].Name);
        Assert.Equal(TotalsCalculation.Sum, table.Columns[3].TotalsCalculation);
        Assert.Equal("T_H3", ws.Cells[4, 4].Value);
        Assert.Equal("=SUBTOTAL(109,E6:E7)", ws.Cells[7, 4].Formula);
    }

    [Fact]
    public void InsertColumn_AfterTable_LeavesTableUnchanged()
    {
        var ws = NewSheet();
        var table = AddTable(ws, "T", "B5:D8");

        ws.InsertColumn(4);

        Assert.Equal(RangeRef.Parse("B5:D8"), table.Range);
        Assert.Equal(3, table.Columns.Count);
    }

    [Fact]
    public void DeleteColumn_BeforeTable_MovesTableLeft()
    {
        var ws = NewSheet();
        var table = AddTable(ws, "T", "B5:D8");

        ws.DeleteColumn(0);

        Assert.Equal(RangeRef.Parse("A5:C8"), table.Range);
        Assert.Equal("T_H1", ws.Cells[4, 0].Value);
    }

    [Fact]
    public void DeleteColumn_InsideTable_RemovesColumn()
    {
        var ws = NewSheet();
        var table = AddTable(ws, "T", "B5:D8");
        table.Columns[2].Name = "Amount";

        ws.DeleteColumn(2);

        Assert.Equal(RangeRef.Parse("B5:C8"), table.Range);
        Assert.Equal(2, table.Columns.Count);
        Assert.Equal([0, 1], table.Columns.Select(c => c.Index));
        Assert.Equal("Amount", table.Columns[1].Name);
        Assert.Equal("T_H3", ws.Cells[4, 2].Value);
    }

    [Fact]
    public void DeleteColumn_OnlyColumnOfTable_RemovesTable()
    {
        var ws = NewSheet();
        var table = AddTable(ws, "T", "B5:B8");

        ws.DeleteColumn(1);

        Assert.DoesNotContain(table, ws.Tables);
    }

    [Fact]
    public void InsertRowCommand_Undo_RestoresTableRanges()
    {
        var ws = NewSheet();
        var above = AddTable(ws, "Above", "B2:D4", totals: false);
        var below = AddTable(ws, "Below", "B8:D11");

        var command = new InsertRowCommand(ws, 3);
        command.Execute();

        Assert.Equal(RangeRef.Parse("B2:D5"), above.Range);
        Assert.Equal(RangeRef.Parse("B9:D12"), below.Range);

        command.Unexecute();

        Assert.Equal(RangeRef.Parse("B2:D4"), above.Range);
        Assert.Equal(RangeRef.Parse("B8:D11"), below.Range);
    }

    [Fact]
    public void DeleteRowsCommand_Undo_RestoresTableRanges()
    {
        var ws = NewSheet();
        var table = AddTable(ws, "T", "B5:D9");

        var command = new DeleteRowsCommand(ws, 6, 7);
        command.Execute();

        Assert.Equal(RangeRef.Parse("B5:D7"), table.Range);

        command.Unexecute();

        Assert.Equal(RangeRef.Parse("B5:D9"), table.Range);
        Assert.Equal("T_R6C1", ws.Cells[6, 1].Value);
        Assert.Equal("Total T", ws.Cells[8, 1].Value);
    }

    [Fact]
    public void InsertRow_StackedTablesWithTotals_KeepTotalsRowsAligned()
    {
        var ws = NewSheet(40, 10);
        var materials = AddTable(ws, "Table_P", "A5:J7");
        var subcontractors = AddTable(ws, "Table_U", "A13:J15");
        var fabrication = AddTable(ws, "Table_L", "A21:J23");

        ws.InsertRow(5);

        Assert.Equal(RangeRef.Parse("A5:J8"), materials.Range);
        Assert.Equal(RangeRef.Parse("A14:J16"), subcontractors.Range);
        Assert.Equal(RangeRef.Parse("A22:J24"), fabrication.Range);

        foreach (var table in new[] { materials, subcontractors, fabrication })
        {
            var header = table.HeaderRowRange!.Value.Start;
            var totals = table.TotalsRowRange!.Value.Start;
            Assert.Equal($"{table.Name}_H0", ws.Cells[header.Row, header.Column].Value);
            Assert.Equal($"Total {table.Name}", ws.Cells[totals.Row, totals.Column].Value);
        }
    }

    [Fact]
    public void InsertRow_ShiftsSheetAutoFilterRange()
    {
        var ws = NewSheet();
        ws.AutoFilter.Range = RangeRef.Parse("B5:D8");

        ws.InsertRow(2);
        Assert.Equal(RangeRef.Parse("B6:D9"), ws.AutoFilter.Range);

        ws.InsertRow(7);
        Assert.Equal(RangeRef.Parse("B6:D10"), ws.AutoFilter.Range);

        ws.DeleteRow(0);
        Assert.Equal(RangeRef.Parse("B5:D9"), ws.AutoFilter.Range);

        ws.DeleteRow(6);
        Assert.Equal(RangeRef.Parse("B5:D8"), ws.AutoFilter.Range);

        ws.InsertColumn(0);
        Assert.Equal(RangeRef.Parse("C5:E8"), ws.AutoFilter.Range);

        ws.DeleteColumn(3);
        Assert.Equal(RangeRef.Parse("C5:D8"), ws.AutoFilter.Range);
    }

    [Fact]
    public void InsertRow_ShiftedTables_RoundTripThroughXlsx()
    {
        var wb = new Workbook();
        var ws = wb.AddSheet("Sheet1", 40, 10);
        AddTable(ws, "Table_P", "A5:J7");
        AddTable(ws, "Table_U", "A13:J15");

        ws.InsertRow(5);

        using var stream = new System.IO.MemoryStream();
        wb.SaveToStream(stream);
        stream.Position = 0;
        var loaded = Workbook.LoadFromStream(stream).Sheets[0];

        Assert.Equal(RangeRef.Parse("A5:J8"), loaded.Tables[0].Range);
        Assert.Equal(RangeRef.Parse("A14:J16"), loaded.Tables[1].Range);
        Assert.Equal("Total Table_U", loaded.Cells[15, 0].Value);
    }
}
