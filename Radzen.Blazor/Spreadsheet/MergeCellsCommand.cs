using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Command to merge a range of cells.
/// </summary>
public class MergeCellsCommand(Sheet sheet, RangeRef range, bool center = false) : ICommand
{
    private readonly Sheet sheet = sheet;
    private readonly RangeRef range = range;
    private readonly bool center = center;
    private readonly Dictionary<CellRef, (object? Value, string? Formula, Format Format)> savedCells = [];

    /// <inheritdoc/>
    public bool Execute()
    {
        if (range.Start == range.End)
        {
            return false;
        }

        var overlapping = sheet.MergedCells.GetOverlappingRanges(range);
        if (overlapping.Count > 0)
        {
            return false;
        }

        foreach (var cellRef in range.GetCells())
        {
            if (sheet.Cells.TryGet(cellRef.Row, cellRef.Column, out var cell))
            {
                savedCells[cellRef] = (cell.Value, cell.Formula, cell.Format.Clone());

                if (cellRef != range.Start)
                {
                    cell.Value = null;
                }
            }
        }

        sheet.MergedCells.Add(range);

        if (center && sheet.Cells.TryGet(range.Start.Row, range.Start.Column, out var topLeft))
        {
            topLeft.Format = topLeft.Format.WithTextAlign(TextAlign.Center);
        }

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        sheet.MergedCells.Remove(range);

        foreach (var kvp in savedCells)
        {
            if (sheet.Cells.TryGet(kvp.Key.Row, kvp.Key.Column, out var cell))
            {
                cell.Format = kvp.Value.Format.Clone();

                if (kvp.Value.Formula != null)
                {
                    cell.Formula = kvp.Value.Formula;
                }
                else
                {
                    cell.Value = kvp.Value.Value;
                }
            }
        }
    }
}
