using Bunit;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

/// <summary>
/// Tests for the UI staying in sync after a structural edit (row/column insert or delete).
/// These guard the bug where the selection/cells/formula bar only refreshed after an
/// unrelated change (e.g. a scroll), because the structural change either notified the
/// grid against intermediate geometry, never re-bound the cell views, or never told the
/// selection-dependent chrome to re-read the active cell.
/// </summary>
public class InsertDeleteRenderTests : TestContext
{
    [Fact]
    public void InsertColumn_RaisesColumnChange_AfterCellsHaveShifted()
    {
        var sheet = new Worksheet(5, 5);
        sheet.Cells[0, 0].Value = "A1";

        // The grid lays itself out when the column axis raises Changed. By the time that
        // fires, every shift must already be in place — otherwise the grid (and the
        // selection overlay it hosts) renders against stale geometry with nothing to
        // re-render it afterwards.
        object observedAtChange = "unset";
        sheet.Columns.Changed += () => observedAtChange = sheet.Cells[0, 1].Value;

        sheet.InsertColumn(0, 1);

        Assert.Equal("A1", observedAtChange);
    }

    [Fact]
    public void InsertRow_RaisesRowChange_AfterCellsHaveShifted()
    {
        var sheet = new Worksheet(5, 5);
        sheet.Cells[0, 0].Value = "A1";

        object observedAtChange = "unset";
        sheet.Rows.Changed += () => observedAtChange = sheet.Cells[1, 0].Value;

        sheet.InsertRow(0, 1);

        Assert.Equal("A1", observedAtChange);
    }

    [Fact]
    public void InsertColumn_NotifiesSelection_WhenActiveCellAffected()
    {
        var sheet = new Worksheet(5, 5);
        sheet.Selection.Select(new CellRef(0, 2)); // C1

        var changed = 0;
        sheet.Selection.Changed += () => changed++;

        // Inserting before the selected column swaps the cell under the selection, so the
        // formula bar (which only listens to Selection.Changed) must be told to re-read.
        sheet.InsertColumn(2, 1);

        Assert.True(changed > 0);
    }

    [Fact]
    public void InsertRow_NotifiesSelection_WhenActiveCellAffected()
    {
        var sheet = new Worksheet(5, 5);
        sheet.Selection.Select(new CellRef(2, 0)); // A3

        var changed = 0;
        sheet.Selection.Changed += () => changed++;

        sheet.InsertRow(2, 1);

        Assert.True(changed > 0);
    }

    [Fact]
    public void DeleteColumn_NotifiesSelection_WhenActiveCellAffected()
    {
        var sheet = new Worksheet(5, 5);
        sheet.Selection.Select(new CellRef(0, 2)); // C1

        var changed = 0;
        sheet.Selection.Changed += () => changed++;

        new DeleteColumnsCommand(sheet, 2, 2).Execute();

        Assert.True(changed > 0);
    }

    [Fact]
    public void CellView_RefreshesValue_WhenCellShiftedUnderneathSamePosition()
    {
        var sheet = new Worksheet(5, 5);
        sheet.Cells[0, 0].Value = "MARKER"; // A1

        // Render a single cell pinned to grid position (0,0) — exactly how the virtual
        // grid renders it. The position never changes; only the cell underneath does.
        var cut = RenderComponent<CellView>(parameters => parameters
            .Add(c => c.Worksheet, sheet)
            .Add(c => c.Row, 0)
            .Add(c => c.Column, 0));

        Assert.Contains("MARKER", cut.Markup);

        // Insert a column before A: "MARKER" moves to B1 and (0,0) becomes a fresh empty cell.
        sheet.InsertColumn(0, 1);

        // The virtual grid re-renders and re-passes the same Row/Column to this cell.
        cut.SetParametersAndRender(parameters => parameters
            .Add(c => c.Worksheet, sheet)
            .Add(c => c.Row, 0)
            .Add(c => c.Column, 0));

        // (0,0) is empty now, so the stale "MARKER" must be gone.
        Assert.DoesNotContain("MARKER", cut.Markup);
    }
}
