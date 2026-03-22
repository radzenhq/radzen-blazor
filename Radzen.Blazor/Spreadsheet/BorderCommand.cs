using System.Collections.Generic;

using Radzen.Blazor.Spreadsheet;
namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Base class for commands that modify the format of cells in a range, with snapshot/restore for undo.
/// </summary>
/// <summary>
/// Base class for commands that modify the format of cells in a range, with snapshot/restore for undo.
/// </summary>
public abstract class RangeFormatCommandBase(Sheet sheet, RangeRef range) : ICommand
{
    /// <summary>
    /// The sheet being operated on.
    /// </summary>
    protected readonly Sheet sheet = sheet;

    /// <summary>
    /// The range of cells being modified.
    /// </summary>
    protected readonly RangeRef range = range;

    private readonly Dictionary<CellRef, Format> existing = [];

    /// <inheritdoc/>
    public bool Execute()
    {
        existing.Clear();

        foreach (var cellRef in range.GetCells())
        {
            var cell = sheet.Cells[cellRef.Row, cellRef.Column];

            existing[cellRef] = cell.Format.Clone();

            cell.Format = ApplyFormat(cell.Format, cellRef);
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

    /// <summary>
    /// Returns the new format for the given cell. Called once per cell during Execute.
    /// </summary>
    protected abstract Format ApplyFormat(Format current, CellRef cellRef);
}

/// <summary>
/// Command to apply border styles to the edges of a range.
/// </summary>
public class BorderCommand(Sheet sheet, RangeRef range, BorderStyle? top, BorderStyle? right, BorderStyle? bottom, BorderStyle? left)
    : RangeFormatCommandBase(sheet, range)
{
    /// <inheritdoc/>
    protected override Format ApplyFormat(Format current, CellRef cellRef)
    {
        System.ArgumentNullException.ThrowIfNull(current);

        var newFormat = current.Clone();

        if (cellRef.Row == range.Start.Row && top != null)
        {
            newFormat.BorderTop = top.Clone();
        }

        if (cellRef.Row == range.End.Row && bottom != null)
        {
            newFormat.BorderBottom = bottom.Clone();
        }

        if (cellRef.Column == range.Start.Column && left != null)
        {
            newFormat.BorderLeft = left.Clone();
        }

        if (cellRef.Column == range.End.Column && right != null)
        {
            newFormat.BorderRight = right.Clone();
        }

        return newFormat;
    }
}

/// <summary>
/// Command to apply borders to all edges of all cells in a range.
/// </summary>
public class AllBordersCommand(Sheet sheet, RangeRef range, BorderStyle style)
    : RangeFormatCommandBase(sheet, range)
{
    /// <inheritdoc/>
    protected override Format ApplyFormat(Format current, CellRef cellRef)
    {
        System.ArgumentNullException.ThrowIfNull(current);

        var newFormat = current.Clone();
        newFormat.BorderTop = style.Clone();
        newFormat.BorderRight = style.Clone();
        newFormat.BorderBottom = style.Clone();
        newFormat.BorderLeft = style.Clone();
        return newFormat;
    }
}

/// <summary>
/// Command to clear all borders from cells in a range.
/// </summary>
public class NoBordersCommand(Sheet sheet, RangeRef range)
    : RangeFormatCommandBase(sheet, range)
{
    /// <inheritdoc/>
    protected override Format ApplyFormat(Format current, CellRef cellRef)
    {
        System.ArgumentNullException.ThrowIfNull(current);

        var newFormat = current.Clone();
        newFormat.BorderTop = null;
        newFormat.BorderRight = null;
        newFormat.BorderBottom = null;
        newFormat.BorderLeft = null;
        return newFormat;
    }
}
