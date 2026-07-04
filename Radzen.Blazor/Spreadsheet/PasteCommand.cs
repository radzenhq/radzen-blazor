using System;
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

// Snapshots overwritten destination cells (and, for a same-sheet cut, the cleared source cells)
// before the write, and captures the pasted result so redo can replay it without depending on
// the live clipboard. Batching comes from the UndoRedoStack.
class PasteCommand : RangeSnapshotCommandBase
{
    private static readonly string[] LineSeparators = ["\r\n", "\r", "\n"];
    
    private readonly SpreadsheetClipboard clipboard;
    private readonly RangeRef destination;
    private readonly string? text;
    private readonly Dictionary<CellRef, (object? value, string? formula, Format? format)> result = [];
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
            // Redo: replay the captured result instead of re-running the (possibly consumed) clipboard.
            Restore(result);
            return true;
        }

        if (!string.IsNullOrEmpty(text) && !destination.Collapsed)
        {
            return ExecuteTiledTextPaste();
        }

        var destinationRange = clipboard.GetPasteRange(sheet, destination.Start, text);

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
            clipboard.Paste(sheet, destination.Start, text);
        }
        else
        {
            clipboard.Paste(sheet, destination.Start);
        }

        CaptureResult();
        pasted = true;
        return true;
    }

    private bool ExecuteTiledTextPaste()
    {
        var rows = text!.Split(LineSeparators, StringSplitOptions.None);
        var data = rows.Select(r => r.Split('\t')).ToArray();

        int dataRowCount = data.Length;
        if (dataRowCount > 0 && data[dataRowCount - 1].Length > 0 && string.IsNullOrEmpty(data[dataRowCount - 1][0]))
        {
            dataRowCount--;
        }

        if (dataRowCount == 0)
        {
            return false;
        }

        int dataColCount = data[0].Length;
        int startRow = destination.Start.Row;
        int startCol = destination.Start.Column;
        int endRow = destination.End.Row;
        int endCol = destination.End.Column;

        var cellsToCapture = new List<CellRef>();
        for (int r = startRow; r <= endRow; r++)
        {
            for (int c = startCol; c <= endCol; c++)
            {
                cellsToCapture.Add(new CellRef(r, c));
            }
        }

        CaptureRange(cellsToCapture);

        for (int r = startRow; r <= endRow; r++)
        {
            for (int c = startCol; c <= endCol; c++)
            {
                var cellRef = new CellRef(r, c);
                if (sheet.IsCellEditable(cellRef))
                {
                    int dataRowIndex = (r - startRow) % dataRowCount;
                    int dataColIndex = (c - startCol) % dataColCount;
                    sheet.Cells[r, c].Value = data[dataRowIndex][dataColIndex];
                }
            }
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