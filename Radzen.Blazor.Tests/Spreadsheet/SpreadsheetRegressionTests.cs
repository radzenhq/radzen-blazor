using Xunit;

using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet.Tests;

// Regressions found during end-user testing of RadzenSpreadsheet.
public class SpreadsheetRegressionTests
{
    // #1 - A batched value edit must still notify the edited cell (regression from wrapping
    // every command in Worksheet.Batch). Dependents were handled by EndUpdate; the changed
    // value cell was not.
    [Fact]
    public void BatchedValueEdit_NotifiesEditedCell()
    {
        var sheet = new Worksheet(10, 10);
        var changed = 0;
        sheet.Cells[2, 2].Changed += _ => changed++;

        sheet.Batch(() => sheet.Cells[2, 2].Value = 5d);

        Assert.Equal(5d, sheet.Cells[2, 2].Value);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void ClearContentsViaStack_NotifiesClearedCell()
    {
        var sheet = new Worksheet(10, 10);
        sheet.Cells[0, 0].Value = 7d;
        var stack = new UndoRedoStack(sheet);

        var changed = 0;
        sheet.Cells[0, 0].Changed += _ => changed++;

        stack.Execute(new ClearContentsCommand(sheet, new RangeRef(new CellRef(0, 0), new CellRef(0, 0))));

        Assert.Null(sheet.Cells[0, 0].Value);
        Assert.True(changed >= 1, "cleared cell should raise Changed");
    }

    // #2 - Sorting a table must never move data into the header (or totals) row.
    [Fact]
    public void TableSort_KeepsHeaderRow()
    {
        var wb = new Workbook();
        var ws = wb.AddSheet("S", 10, 2);
        ws.Cells[0, 0].SetValue("Name");
        ws.Cells[0, 1].SetValue("Dept");
        ws.Cells[1, 0].SetValue("Alice");
        ws.Cells[2, 0].SetValue("Carol");
        ws.Cells[3, 0].SetValue("Tina");

        var table = ws.AddTable("T", RangeRef.Parse("A1:B4"));
        table.Sort(SortOrder.Descending, 0);

        Assert.Equal("Name", ws.Cells[0, 0].Value);   // header intact
        Assert.Equal("Tina", ws.Cells[1, 0].Value);   // first data row (descending)
        Assert.Equal("Carol", ws.Cells[2, 0].Value);
        Assert.Equal("Alice", ws.Cells[3, 0].Value);
    }

    // #6 - A sort must re-apply active filters by value, so filtered-out rows stay hidden.
    private static Worksheet BuildFilteredSheet()
    {
        var wb = new Workbook();
        var ws = wb.AddSheet("S", 20, 2);
        ws.Cells[0, 0].SetValue("Name");
        ws.Cells[0, 1].SetValue("Dept");
        var data = new[]
        {
            ("Alice", "Eng"), ("Bob", "Mkt"), ("Carol", "Eng"),
            ("Dave", "Sales"), ("Tina", "Mkt"), ("Sam", "Eng"),
        };
        for (var i = 0; i < data.Length; i++)
        {
            ws.Cells[i + 1, 0].SetValue(data[i].Item1);
            ws.Cells[i + 1, 1].SetValue(data[i].Item2);
        }

        ws.AutoFilter.Range = new RangeRef(new CellRef(0, 0), new CellRef(6, 1));
        ws.AutoFilter.ApplyValueFilter(1, ["Eng"]);
        return ws;
    }

    [Fact]
    public void SortPreservesActiveFilter()
    {
        var ws = BuildFilteredSheet();

        ws.Sort(new RangeRef(new CellRef(1, 0), new CellRef(6, 1)),
            new SortKey { ColumnIndex = 0, Order = SortOrder.Descending });

        var visible = 0;
        for (var r = 1; r <= 6; r++)
        {
            if (!ws.Rows.IsHidden(r))
            {
                visible++;
                Assert.Equal("Eng", ws.Cells[r, 1].Value);
            }
        }

        Assert.Equal(3, visible);   // Alice, Carol, Sam
    }

    [Fact]
    public void UndoSortRestoresFilter()
    {
        var ws = BuildFilteredSheet();
        var stack = new UndoRedoStack(ws);

        stack.Execute(new SortCommand(ws, new RangeRef(new CellRef(1, 0), new CellRef(6, 1)),
            SortOrder.Descending, 0));
        stack.Undo();

        // Original order restored; filter re-applied by value -> only the Eng rows visible.
        Assert.Equal("Alice", ws.Cells[1, 0].Value);
        Assert.False(ws.Rows.IsHidden(1)); // Alice / Eng
        Assert.True(ws.Rows.IsHidden(2));  // Bob / Mkt
        Assert.False(ws.Rows.IsHidden(3)); // Carol / Eng
        Assert.True(ws.Rows.IsHidden(4));  // Dave / Sales
        Assert.True(ws.Rows.IsHidden(5));  // Tina / Mkt
        Assert.False(ws.Rows.IsHidden(6)); // Sam / Eng
    }

    // #3 - the now-enabled Merge button's action must merge the selected range (the command
    // the toolbar dispatches: MergeCellsCommand over Selection.Range).
    [Fact]
    public void MergeCommand_MergesSelectionRange()
    {
        var sheet = new Worksheet(10, 10);
        var stack = new UndoRedoStack(sheet);
        var range = new RangeRef(new CellRef(4, 5), new CellRef(5, 6)); // F5:G6
        sheet.Selection.Select(range);

        stack.Execute(new MergeCellsCommand(sheet, sheet.Selection.Range, center: true));

        Assert.Equal(range, sheet.MergedCells.GetMergedRange(new CellRef(4, 5)));

        stack.Undo();
        Assert.Equal(RangeRef.Invalid, sheet.MergedCells.GetMergedRange(new CellRef(4, 5)));
    }
}
