using System.Collections.Generic;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Pastes clipboard content (an internal range or external delimited text) into the worksheet and
/// makes it undoable: the overwritten destination cells - and, for a same-sheet cut, the cleared
/// source cells - are snapshotted before the write, and the pasted result is captured so redo can
/// replay it without depending on the live clipboard. Batching comes from the UndoRedoStack.
/// </summary>
class PasteCommand : RangeSnapshotCommandBase
{
    private readonly SpreadsheetClipboard clipboard;
    private readonly CellRef destination;
    private readonly string? text;
    private readonly Dictionary<CellRef, (object? value, string? formula, Format? format)> result = [];
    private bool pasted;

    /// <inheritdoc/>
    public override SheetAction RequiredAction => SheetAction.EditCell;

    public PasteCommand(SpreadsheetClipboard clipboard, Worksheet sheet, CellRef destination, string? text)
        : base(sheet)
    {
        this.clipboard = clipboard;
        this.destination = destination;
        this.text = text;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
