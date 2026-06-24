using System;
using System.Text;

using Radzen.Documents.Spreadsheet;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

enum ClipboardOperation
{
    Copy,
    Move
}

class SpreadsheetClipboard
{
    private RangeRef? range;
    private Worksheet? sheet;
    private ClipboardOperation operation;
    private string? csv;

    public void Copy(Worksheet sheet)
    {
        this.sheet = sheet;
        range = sheet.Selection.Range;
        operation = ClipboardOperation.Copy;
        csv = sheet.GetDelimitedString(range.Value);
    }

    public void Cut(Worksheet sheet)
    {
        this.sheet = sheet;
        range = sheet.Selection.Range;
        operation = ClipboardOperation.Move;
        csv = sheet.GetDelimitedString(range.Value);
    }

    public void Paste(Worksheet targetSheet, CellRef destinationStart)
    {
        if (range.HasValue && sheet is not null)
        {
            var adjustment = operation == ClipboardOperation.Copy ? FormulaAdjustment.AdjustRelative : FormulaAdjustment.Preserve;
            targetSheet.PasteRange(sheet, range.Value, destinationStart, adjustment);

            if (operation == ClipboardOperation.Move)
            {
                Clear(sheet, range.Value);
                ClearInternal();
            }
        }
    }

    public bool TryPaste(Worksheet targetSheet, CellRef destinationStart, string pastedText)
    {
        if (range.HasValue && sheet is not null && !string.IsNullOrEmpty(csv) && pastedText == csv)
        {
            Paste(targetSheet, destinationStart);
            return true;
        }

        // clear internal clipboard when external data is pasted
        ClearInternal();
        return false;
    }

    public void Paste(Worksheet targetSheet, CellRef destinationStart, string pastedText)
    {
        if (!TryPaste(targetSheet, destinationStart, pastedText))
        {
            targetSheet.InsertDelimitedString(destinationStart, pastedText);
        }
    }

    public RangeRef GetPasteRange(Worksheet targetSheet, CellRef destinationStart, string? pastedText)
    {
        if (range.HasValue && sheet is not null && !string.IsNullOrEmpty(csv) && (pastedText is null || pastedText == csv))
        {
            var source = range.Value;
            return new RangeRef(destinationStart, new CellRef(destinationStart.Row + source.Rows - 1, destinationStart.Column + source.Columns - 1));
        }

        if (string.IsNullOrEmpty(pastedText))
        {
            return RangeRef.Invalid;
        }

        var lines = pastedText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var rowCount = Math.Min(lines.Length, targetSheet.RowCount - destinationStart.Row);

        var columnCount = 0;
        foreach (var line in lines)
        {
            columnCount = Math.Max(columnCount, line.Split('\t').Length);
        }
        columnCount = Math.Min(columnCount, targetSheet.ColumnCount - destinationStart.Column);

        if (rowCount <= 0 || columnCount <= 0)
        {
            return RangeRef.Invalid;
        }

        return new RangeRef(destinationStart, new CellRef(destinationStart.Row + rowCount - 1, destinationStart.Column + columnCount - 1));
    }

    // Cross-sheet moves are not covered: per-sheet undo stacks can't snapshot a source on another sheet.
    public bool TryGetMoveSource(Worksheet targetSheet, out RangeRef source)
    {
        if (operation == ClipboardOperation.Move && range.HasValue && ReferenceEquals(sheet, targetSheet))
        {
            source = range.Value;
            return true;
        }

        source = RangeRef.Invalid;
        return false;
    }

    private void ClearInternal()
    {
        range = null;
        sheet = null;
        csv = null;
        operation = ClipboardOperation.Copy;
    }

    private static void Clear(Worksheet sourceSheet, RangeRef source)
    {
        for (var sr = source.Start.Row; sr <= source.End.Row; sr++)
        {
            for (var sc = source.Start.Column; sc <= source.End.Column; sc++)
            {
                if (sourceSheet.Cells.TryGet(sr, sc, out var srcCell))
                {
                    srcCell.Formula = null;
                    srcCell.Value = null;
                }
            }
        }
    }
}