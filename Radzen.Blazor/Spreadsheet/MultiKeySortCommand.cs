using System;
using System.Collections.Generic;
using Radzen.Documents.Spreadsheet;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Sort a range using one or more <see cref="SortKey"/> levels. Supports undo/redo by
/// snapshotting affected cells before sorting.
/// </summary>
public class MultiKeySortCommand(Worksheet sheet, RangeRef range, SortKey[] keys, bool skipHeaderRow = false) : ICommand, IProtectedCommand
{
    /// <inheritdoc/>
    public SheetAction RequiredAction => SheetAction.Sort;

    private readonly Worksheet sheet = sheet ?? throw new ArgumentNullException(nameof(sheet));
    private readonly RangeRef range = range;
    private readonly SortKey[] keys = keys ?? throw new ArgumentNullException(nameof(keys));
    private readonly bool skipHeaderRow = skipHeaderRow;
    private readonly Dictionary<CellRef, (object? value, string? formula, Format? format)> cells = [];

    /// <inheritdoc/>
    public bool Execute()
    {
        if (range == RangeRef.Invalid || keys.Length == 0) return false;

        Snapshot();

        var startRow = range.Start.Row + (skipHeaderRow ? 1 : 0);
        var sortRange = new RangeRef(new CellRef(startRow, range.Start.Column), range.End);
        sheet.Sort(sortRange, keys);

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        if (range == RangeRef.Invalid) return;

        foreach (var (cellRef, (value, formula, format)) in cells)
        {
            var target = sheet.Cells[cellRef];
            target.Value = value;
            target.Formula = formula;
            if (format is not null) target.Format = format;
        }
    }

    private void Snapshot()
    {
        cells.Clear();
        var startRow = range.Start.Row + (skipHeaderRow ? 1 : 0);
        for (var row = startRow; row <= range.End.Row; row++)
        {
            for (var col = range.Start.Column; col <= range.End.Column; col++)
            {
                var addr = new CellRef(row, col);
                var cell = sheet.Cells[addr];
                cells[addr] = (cell.Value, cell.Formula, cell.Format?.Clone());
            }
        }
    }
}
