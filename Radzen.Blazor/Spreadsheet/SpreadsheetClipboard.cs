using System.Text;

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
    private Sheet? sheet;
    private ClipboardOperation operation;
    private string? csv;

    public void Copy(Sheet sheet)
    {
        this.sheet = sheet;
        range = sheet.Selection.Range;
        operation = ClipboardOperation.Copy;
        csv = sheet.GetDelimitedString(range.Value);
    }

    public void Cut(Sheet sheet)
    {
        this.sheet = sheet;
        range = sheet.Selection.Range;
        operation = ClipboardOperation.Move;
        csv = sheet.GetDelimitedString(range.Value);
    }

    public void Paste(Sheet targetSheet, CellRef destinationStart)
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

    public bool TryPaste(Sheet targetSheet, CellRef destinationStart, string pastedText)
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

    public void Paste(Sheet targetSheet, CellRef destinationStart, string pastedText)
    {
        if (!TryPaste(targetSheet, destinationStart, pastedText))
        {
            targetSheet.InsertDelimitedString(destinationStart, pastedText);
        }
    }

    private void ClearInternal()
    {
        range = null;
        sheet = null;
        csv = null;
        operation = ClipboardOperation.Copy;
    }

    private static void Clear(Sheet sourceSheet, RangeRef source)
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