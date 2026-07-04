using Xunit;
using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet.Tests;

/// <summary>
/// Paste/cut now flow through PasteCommand, so they are undoable and batched (via the UndoRedoStack).
/// </summary>
public class PasteCommandTests
{
    [Fact]
    public void ExternalTextPaste_IsUndoable()
    {
        var sheet = new Worksheet(20, 5);
        sheet.Cells[0, 0].Value = "orig";

        var clipboard = new SpreadsheetClipboard();
        var stack = new UndoRedoStack(sheet);

        stack.Execute(new PasteCommand(clipboard, sheet, new RangeRef(new CellRef(0, 0), new CellRef(0, 0)), "X\tY"));

        Assert.Equal("X", sheet.Cells[0, 0].Value);
        Assert.Equal("Y", sheet.Cells[0, 1].Value);

        stack.Undo();

        Assert.Equal("orig", sheet.Cells[0, 0].Value);
        Assert.Null(sheet.Cells[0, 1].Value);
    }

    [Fact]
    public void InternalCopyPaste_IsUndoableAndBatched()
    {
        var sheet = new Worksheet(20, 5);
        sheet.Cells[0, 0].Value = 10d;
        sheet.Cells[1, 0].Value = 20d;
        sheet.Cells[2, 0].Value = 30d;
        sheet.Cells[10, 3].Formula = "=SUM(B1:B3)"; // depends on the paste destination
        sheet.Cells[10, 3].Formula = "=SUM(B1:B3)";

        var clipboard = new SpreadsheetClipboard();
        sheet.Selection.Select(new RangeRef(new CellRef(0, 0), new CellRef(2, 0)));
        clipboard.Copy(sheet);

        var evals = 0;
        sheet.Cells[10, 3].Changed += _ => evals++;

        var stack = new UndoRedoStack(sheet);
        stack.Execute(new PasteCommand(clipboard, sheet, new RangeRef(new CellRef(0, 1), new CellRef(0, 1)), null));

        Assert.Equal(60d, sheet.Cells[10, 3].Value);
        Assert.True(evals <= 2);

        stack.Undo();
        Assert.Equal(0d, sheet.Cells[10, 3].Value);
    }

    [Fact]
    public void CutPaste_MovesSourceAndUndoRestoresBoth()
    {
        var sheet = new Worksheet(20, 5);
        sheet.Cells[0, 0].Value = "A";
        sheet.Cells[1, 0].Value = "B";

        var clipboard = new SpreadsheetClipboard();
        sheet.Selection.Select(new RangeRef(new CellRef(0, 0), new CellRef(1, 0))); // A1:A2
        clipboard.Cut(sheet);

        var stack = new UndoRedoStack(sheet);
        stack.Execute(new PasteCommand(clipboard, sheet, new RangeRef(new CellRef(0, 2), new CellRef(0, 2)), null));

        Assert.Equal("A", sheet.Cells[0, 2].Value);
        Assert.Equal("B", sheet.Cells[1, 2].Value);
        Assert.Null(sheet.Cells[0, 0].Value);
        Assert.Null(sheet.Cells[1, 0].Value);

        stack.Undo();

        Assert.Equal("A", sheet.Cells[0, 0].Value);
        Assert.Equal("B", sheet.Cells[1, 0].Value);
        Assert.Null(sheet.Cells[0, 2].Value);
        Assert.Null(sheet.Cells[1, 2].Value);
    }

    [Fact]
    public void CutPaste_RedoReplays_EvenThoughClipboardWasConsumed()
    {
        var sheet = new Worksheet(20, 5);
        sheet.Cells[0, 0].Value = "A";

        var clipboard = new SpreadsheetClipboard();
        sheet.Selection.Select(new CellRef(0, 0));
        clipboard.Cut(sheet);

        var stack = new UndoRedoStack(sheet);
        stack.Execute(new PasteCommand(clipboard, sheet, new RangeRef(new CellRef(0, 2), new CellRef(0, 2)), null));
        stack.Undo();

        Assert.Equal("A", sheet.Cells[0, 0].Value);
        Assert.Null(sheet.Cells[0, 2].Value);

        stack.Redo(); // clipboard was cleared by the cut-paste; redo replays from captured result
        stack.Redo();

        Assert.Equal("A", sheet.Cells[0, 2].Value);
        Assert.Null(sheet.Cells[0, 0].Value);
    }
    
    [Fact]
    public void TiledTextPaste_FillsSelectedRange()
    {
        var sheet = new Worksheet(20, 5);
        var clipboard = new SpreadsheetClipboard();
        var stack = new UndoRedoStack(sheet);
        
        var destination = new RangeRef(new CellRef(0, 0), new CellRef(1, 1));
        
        stack.Execute(new PasteCommand(clipboard, sheet, destination, "X"));

        Assert.Equal("X", sheet.Cells[0, 0].Value);
        Assert.Equal("X", sheet.Cells[0, 1].Value);
        Assert.Equal("X", sheet.Cells[1, 0].Value);
        Assert.Equal("X", sheet.Cells[1, 1].Value);

        stack.Undo();

        Assert.Null(sheet.Cells[0, 0].Value);
        Assert.Null(sheet.Cells[0, 1].Value);
        Assert.Null(sheet.Cells[1, 0].Value);
        Assert.Null(sheet.Cells[1, 1].Value);
    }
}