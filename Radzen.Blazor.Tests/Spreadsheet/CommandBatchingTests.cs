using Xunit;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet.Tests;

/// <summary>
/// The UndoRedoStack wraps every command's Execute/Undo/Redo in a single update batch, so even
/// commands that do not batch themselves (e.g. ClearContentsCommand writes cells in a loop)
/// recalc dependent formulas once instead of once per written cell.
/// </summary>
public class CommandBatchingTests
{
    private static Worksheet SheetWithAggregate(out UndoRedoStack stack)
    {
        var sheet = new Worksheet(20, 5);
        for (var r = 0; r < 8; r++)
        {
            sheet.Cells[r, 0].Value = (double)(r + 1); // A1..A8 = 1..8
        }
        sheet.Cells[10, 3].Formula = "=SUM(A1:A8)"; // D11, outside the cleared range
        stack = new UndoRedoStack(sheet);
        return sheet;
    }

    [Fact]
    public void ClearViaStack_RecalcsAggregateOnce()
    {
        var sheet = SheetWithAggregate(out var stack);

        var evals = 0;
        sheet.Cells[10, 3].Changed += _ => evals++;

        stack.Execute(new ClearContentsCommand(sheet, new RangeRef(new CellRef(0, 0), new CellRef(7, 0))));

        Assert.Equal(0d, sheet.Cells[10, 3].Value); // every input cleared -> SUM == 0
        Assert.True(evals <= 2, $"aggregate re-evaluated {evals} times on Execute; expected <= 2 (batched at the stack)");
    }

    [Fact]
    public void UndoClearViaStack_RestoresAndRecalcsOnce()
    {
        var sheet = SheetWithAggregate(out var stack);
        stack.Execute(new ClearContentsCommand(sheet, new RangeRef(new CellRef(0, 0), new CellRef(7, 0))));

        var evals = 0;
        sheet.Cells[10, 3].Changed += _ => evals++;

        stack.Undo();

        Assert.Equal(36d, sheet.Cells[10, 3].Value); // 1+2+...+8 restored
        Assert.True(evals <= 2, $"aggregate re-evaluated {evals} times on Undo; expected <= 2 (batched at the stack)");
    }
}
