using System.Collections.Generic;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

// Snapshots overwritten destination cells (and, for a same-sheet cut, the cleared source cells)
// before the write, and captures the pasted result so redo can replay it without depending on
// the live clipboard. Batching comes from the UndoRedoStack.
class PasteCommand : RangeSnapshotCommandBase
{
    private readonly SpreadsheetClipboard clipboard;
    private readonly CellRef destination;
    private readonly string? text;
    private readonly Dictionary<CellRef, (object? value, string? formula, Format? format)> result = [];
    private bool pasted;

    public override SheetAction RequiredAction => SheetAction.EditCell;

    public PasteCommand(SpreadsheetClipboard clipboard, Worksheet sheet, CellRef destination, string? text)
        : base(sheet)
    {
        this.clipboard = clipboard;
        this.destination = destination;
        this.text = text;
    }

    protected override bool DoExecute()
    {
        if (pasted)
        {
            // Redo: replay the captured result instead of re-running the (possibly consumed) clipboard.
            Restore(result);
            return true;
        }

        var destinationRange = clipboard.GetPasteRange(sheet, destination, text);

        if (destinationRange == RangeRef.Invalid)
        {
            return false;
        }

        CaptureRange(destinationRange.GetCells());

        if (clipboard.TryGetMoveSource(sheet, out var moveSource))
        {
            CaptureRange(moveSource.GetCells());
        }

        if (text is not null)
        {
            clipboard.Paste(sheet, destination, text);
        }
        else
        {
            clipboard.Paste(sheet, destination);
        }

        CaptureResult();
        pasted = true;
        return true;
    }

    public override void Unexecute() => RestoreSnapshot();

    private void CaptureResult()
    {
        foreach (var cellRef in snapshot.Keys)
        {
            string? formula = null;
            object? value = null;
            Format? format = null;

            if (sheet.Cells.TryGet(cellRef.Row, cellRef.Column, out var cell))
            {
                value = cell.Value;
                formula = cell.Formula;
                format = cell.Format?.Clone();
            }

            result[cellRef] = (value, formula, format);
        }
    }
}
