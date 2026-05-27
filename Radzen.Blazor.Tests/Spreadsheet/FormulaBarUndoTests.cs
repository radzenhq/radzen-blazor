using System.Threading.Tasks;
using Bunit;
using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

/// <summary>
/// Known-failing: after undoing a column insert, the formula bar still shows the stale
/// (empty) value even though the active cell's content was restored. The snapshot undo
/// restores cell values AFTER the structural delete, and the formula bar only re-reads on
/// Selection.Changed, so nothing tells it to refresh once the value comes back. It
/// self-corrects on the next click, but should update on undo.
/// </summary>
public class FormulaBarUndoTests : TestContext
{
    [Fact]
    public async Task FormulaBar_ShowsRestoredValue_AfterUndoOfInsertColumn()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;

        var view = new SheetView(new Worksheet(5, 5));
        var sheet = view.Worksheet;
        sheet.Cells[0, 2].Value = "X"; // C1
        sheet.Selection.Select(new CellRef(0, 2));

        // Render the formula bar so it reacts to selection changes as it does in the app.
        var cut = RenderComponent<FormulaEditor>(parameters => parameters
            .Add(p => p.Worksheet, sheet)
            .Add(p => p.Editor, view.Editor));

        // It shows the active cell's value.
        Assert.Equal("X", view.Editor.Value);

        var command = new InsertColumnCommand(sheet, 2);

        // Drive the edits on the renderer's dispatcher so the formula bar can re-render.
        await cut.InvokeAsync(() => command.Execute());   // C1 empties (X shifts to D1)
        await cut.InvokeAsync(() => command.Unexecute());  // undo restores C1 = "X"

        // The active cell is C1 again with "X" restored, so the formula bar must show it.
        Assert.Equal("X", view.Editor.Value);
    }
}
