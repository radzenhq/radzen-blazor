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
    private readonly RangeRef destination;
    private readonly string? text;
    private readonly Dictionary<CellRef, Cell?> result = [];
    private bool pasted;

    public override SheetAction RequiredAction => SheetAction.EditCell;

    public PasteCommand(SpreadsheetClipboard clipboard, Worksheet sheet, RangeRef destination, string? text)
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
            // Redo: replay the captured result instead of re-running the (possibly consumed)
            // clipboard. Execute cleared the snapshot, so re-capture it first or the next
            // undo would have nothing to restore.
            foreach (var cellRef in result.Keys)
            {
                Capture(cellRef);
            }

            Restore(result);
            return true;
        }

        var destinationRange = clipboard.GetPasteRange(sheet, destination, text);

        if (destinationRange == RangeRef.Invalid)
        {
            return false;
        }
        
        if (sheet.Protection.IsProtected)
        {
            for (int r = destinationRange.Start.Row; r <= destinationRange.End.Row; r++)
            {
                for (int c = destinationRange.Start.Column; c <= destinationRange.End.Column; c++)
                {
                    if (!sheet.IsCellEditable(new CellRef(r, c)))
                    {
                        return false;
                    }
                }
            }
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
            result[cellRef] = sheet.Cells.TryGet(cellRef.Row, cellRef.Column, out var cell) ? cell.Clone() : null;
        }
    }
}
