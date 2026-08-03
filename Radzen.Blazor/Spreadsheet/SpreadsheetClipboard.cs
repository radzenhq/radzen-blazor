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
    public event Action? Changed;

    public void Copy(Worksheet sheet)
    {
        this.sheet = sheet;
        range = sheet.Selection.Range;
        operation = ClipboardOperation.Copy;
        csv = sheet.GetDelimitedString(range.Value);
        Changed?.Invoke();
    }

    public void Cut(Worksheet sheet)
    {
        this.sheet = sheet;
        range = sheet.Selection.Range;
        operation = ClipboardOperation.Move;
        csv = sheet.GetDelimitedString(range.Value);
        Changed?.Invoke();
    }

    public void Paste(Worksheet targetSheet, RangeRef destination)
    {
        if (range.HasValue && sheet is not null)
        {
            var adjustment = operation == ClipboardOperation.Copy ? FormulaAdjustment.AdjustRelative : FormulaAdjustment.Preserve;
            var source = range.Value;

            if (operation == ClipboardOperation.Copy &&
                !destination.Collapsed &&
                destination.Rows % source.Rows == 0 &&
                destination.Columns % source.Columns == 0)
            {
                for (var r = 0; r < destination.Rows; r += source.Rows)
                {
                    for (var c = 0; c < destination.Columns; c += source.Columns)
                    {
                        var tileStart = new CellRef(destination.Start.Row + r, destination.Start.Column + c);
                        targetSheet.PasteRange(sheet, source, tileStart, adjustment);
                    }
                }
            }
            else
            {
                targetSheet.PasteRange(sheet, source, destination.Start, adjustment);
            }

            if (operation == ClipboardOperation.Move)
            {
                ClearRange(sheet, source);
                Clear();
            }
        }
    }

    public bool TryPaste(Worksheet targetSheet, RangeRef destination, string pastedText)
    {
        if (range.HasValue && sheet is not null && !string.IsNullOrEmpty(csv) && pastedText == csv)
        {
            Paste(targetSheet, destination);
            return true;
        }

        // clear internal clipboard when external data is pasted
        Clear();
        return false;
    }

    public void Paste(Worksheet targetSheet, RangeRef destination, string pastedText)
    {
        if (!TryPaste(targetSheet, destination, pastedText))
        {
            targetSheet.InsertDelimitedString(destination.Start, pastedText);
        }
    }

    public RangeRef GetPasteRange(Worksheet targetSheet, RangeRef destination, string? pastedText)
    {
        if (range.HasValue && sheet is not null && !string.IsNullOrEmpty(csv) && (pastedText is null || pastedText == csv))
        {
            var source = range.Value;
            
            if (operation == ClipboardOperation.Copy &&
                !destination.Collapsed &&
                destination.Rows % source.Rows == 0 &&
                destination.Columns % source.Columns == 0)
            {
                return destination;
            }

            return new RangeRef(destination.Start, new CellRef(destination.Start.Row + source.Rows - 1, destination.Start.Column + source.Columns - 1));
        }

        if (string.IsNullOrEmpty(pastedText))
        {
            return RangeRef.Invalid;
        }

        var lines = pastedText.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var rowCount = Math.Min(lines.Length, targetSheet.RowCount - destination.Start.Row);

        var columnCount = 0;
        foreach (var line in lines)
        {
            columnCount = Math.Max(columnCount, line.Split('\t').Length);
        }
        columnCount = Math.Min(columnCount, targetSheet.ColumnCount - destination.Start.Column);

        if (rowCount <= 0 || columnCount <= 0)
        {
            return RangeRef.Invalid;
        }

        return new RangeRef(destination.Start, new CellRef(destination.Start.Row + rowCount - 1, destination.Start.Column + columnCount - 1));
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
    
    public RangeRef GetSource(Worksheet targetSheet) =>
        range.HasValue && ReferenceEquals(sheet, targetSheet) ? range.Value : RangeRef.Invalid;
    
    public void Clear()
    {
        var rangeHadValue = range.HasValue;
        range = null;
        sheet = null;
        csv = null;
        operation = ClipboardOperation.Copy;
        if (rangeHadValue)
        {
            Changed?.Invoke();
        }
    }

    private static void ClearRange(Worksheet sourceSheet, RangeRef source)
    {
        for (var sr = source.Start.Row; sr <= source.End.Row; sr++)
        {
            for (var sc = source.Start.Column; sc <= source.End.Column; sc++)
            {
                if (sourceSheet.Cells.TryGet(sr, sc, out var srcCell))
                {
                    srcCell.Clear();
                }
            }
        }
    }
}