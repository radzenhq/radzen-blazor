using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Command to apply border styles to a selection of cells.
/// </summary>
public class BorderCommand(Sheet sheet, RangeRef range, BorderStyle? top, BorderStyle? right, BorderStyle? bottom, BorderStyle? left) : ICommand
{
    private readonly Sheet sheet = sheet;
    private readonly RangeRef range = range;
    private readonly Dictionary<CellRef, Format> existing = [];

    /// <inheritdoc/>
    public bool Execute()
    {
        existing.Clear();

        foreach (var cellRef in range.GetCells())
        {
            if (sheet.Cells.TryGet(cellRef.Row, cellRef.Column, out var cell))
            {
                existing[cellRef] = cell.Format.Clone();

                var isTopEdge = cellRef.Row == range.Start.Row;
                var isBottomEdge = cellRef.Row == range.End.Row;
                var isLeftEdge = cellRef.Column == range.Start.Column;
                var isRightEdge = cellRef.Column == range.End.Column;

                var newFormat = cell.Format.Clone();

                if (isTopEdge && top != null)
                {
                    newFormat.BorderTop = top.Clone();
                }

                if (isBottomEdge && bottom != null)
                {
                    newFormat.BorderBottom = bottom.Clone();
                }

                if (isLeftEdge && left != null)
                {
                    newFormat.BorderLeft = left.Clone();
                }

                if (isRightEdge && right != null)
                {
                    newFormat.BorderRight = right.Clone();
                }

                cell.Format = newFormat;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        foreach (var kvp in existing)
        {
            if (sheet.Cells.TryGet(kvp.Key.Row, kvp.Key.Column, out var cell))
            {
                cell.Format = kvp.Value.Clone();
            }
        }
    }
}

/// <summary>
/// Command to apply borders to all edges of all cells in a range.
/// </summary>
public class AllBordersCommand(Sheet sheet, RangeRef range, BorderStyle style) : ICommand
{
    private readonly Sheet sheet = sheet;
    private readonly RangeRef range = range;
    private readonly Dictionary<CellRef, Format> existing = [];

    /// <inheritdoc/>
    public bool Execute()
    {
        existing.Clear();

        foreach (var cellRef in range.GetCells())
        {
            if (sheet.Cells.TryGet(cellRef.Row, cellRef.Column, out var cell))
            {
                existing[cellRef] = cell.Format.Clone();

                var newFormat = cell.Format.Clone();
                newFormat.BorderTop = style.Clone();
                newFormat.BorderRight = style.Clone();
                newFormat.BorderBottom = style.Clone();
                newFormat.BorderLeft = style.Clone();
                cell.Format = newFormat;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        foreach (var kvp in existing)
        {
            if (sheet.Cells.TryGet(kvp.Key.Row, kvp.Key.Column, out var cell))
            {
                cell.Format = kvp.Value.Clone();
            }
        }
    }
}

/// <summary>
/// Command to clear all borders from cells in a range.
/// </summary>
public class NoBordersCommand(Sheet sheet, RangeRef range) : ICommand
{
    private readonly Sheet sheet = sheet;
    private readonly RangeRef range = range;
    private readonly Dictionary<CellRef, Format> existing = [];

    /// <inheritdoc/>
    public bool Execute()
    {
        existing.Clear();

        foreach (var cellRef in range.GetCells())
        {
            if (sheet.Cells.TryGet(cellRef.Row, cellRef.Column, out var cell))
            {
                existing[cellRef] = cell.Format.Clone();

                var newFormat = cell.Format.Clone();
                newFormat.BorderTop = null;
                newFormat.BorderRight = null;
                newFormat.BorderBottom = null;
                newFormat.BorderLeft = null;
                cell.Format = newFormat;
            }
        }

        return true;
    }

    /// <inheritdoc/>
    public void Unexecute()
    {
        foreach (var kvp in existing)
        {
            if (sheet.Cells.TryGet(kvp.Key.Row, kvp.Key.Column, out var cell))
            {
                cell.Format = kvp.Value.Clone();
            }
        }
    }
}
