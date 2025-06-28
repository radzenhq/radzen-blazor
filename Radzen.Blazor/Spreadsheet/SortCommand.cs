using System;
using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents a command to sort a range in a spreadsheet, supporting undo and redo operations.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SortCommand"/> class.
/// </remarks>
/// <param name="sheet">The sheet containing the range to sort.</param>
/// <param name="range">The range to sort.</param>
/// <param name="order">The sort order (ascending or descending).</param>
/// <param name="keyIndex">The column index to sort by.</param>
/// <param name="skipHeaderRow">If true, skips the first row (header) when sorting.</param>
public class SortCommand(Sheet sheet, RangeRef range, SortOrder order, int keyIndex, bool skipHeaderRow = false) : ICommand
{
    private readonly Sheet sheet = sheet ?? throw new ArgumentNullException(nameof(sheet));
    private readonly RangeRef range = range;
    private readonly SortOrder order = order;
    private readonly int keyIndex = keyIndex;
    private readonly bool skipHeaderRow = skipHeaderRow;
    private readonly Dictionary<CellRef, (object? value, string? formula, Format? format)> cells = [];

    /// <inheritdoc/>
    public bool Execute()
    {
        if (range == RangeRef.Invalid)
        {
            return false;
        }

        Store();

        var startRow = range.Start.Row + (skipHeaderRow ? 1 : 0);
        var sortRange = new RangeRef(new CellRef(startRow, range.Start.Column), range.End);
        sheet.Sort(sortRange, order, keyIndex);

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        if (range == RangeRef.Invalid)
        {
            return;
        }

        Restore();
    }

    private void Store()
    {
        cells.Clear();
        var startRow = range.Start.Row + (skipHeaderRow ? 1 : 0);
        for (var row = startRow; row <= range.End.Row; row++)
        {
            for (var column = range.Start.Column; column <= range.End.Column; column++)
            {
                var cellRef = new CellRef(row, column);
                var cell = sheet.Cells[cellRef];
                var format = cell.Format;
                var formatClone = format?.Clone();
                cells[cellRef] = (cell.Value, cell.Formula, formatClone);
            }
        }
    }

    private void Restore()
    {
        foreach (var kvp in cells)
        {
            var cellRef = kvp.Key;
            var (value, formula, format) = kvp.Value;
            var targetCell = sheet.Cells[cellRef];
            targetCell.Value = value;
            targetCell.Formula = formula;
            if (format != null)
            {
                targetCell.Format = format;
            }
        }
    }
} 