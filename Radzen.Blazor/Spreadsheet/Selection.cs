using System;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable
/// <summary>
/// A class for managing the selection of cells or ranges in a spreadsheet.
/// </summary>
/// <param name="sheet"></param>
public class Selection(Sheet sheet)
{
    /// <summary>
    /// Gets or sets the range of cells that are currently selected.
    /// </summary>
    public RangeRef Range { get; private set; } = RangeRef.Invalid;
    /// <summary>
    /// Gets or sets the currently active cell reference.
    /// </summary>
    public CellRef Cell { get; private set; } = CellRef.Invalid;

    /// <summary>
    /// Event that is triggered when the selection changes.
    /// </summary>
    public event Action? Changed;

    private bool pendingChange;

    /// <summary>
    /// Updates the selection to a new cell address and triggers a change event.
    /// </summary>
    /// <param name="address"></param>
    public void Update(CellRef address)
    {
        Cell = address;

        TriggerChange();
    }

    internal void TriggerPendingChange()
    {
        if (pendingChange)
        {
            pendingChange = false;
            Changed?.Invoke();
        }
    }

    private void TriggerChange()
    {
        if (sheet.IsUpdating)
        {
            pendingChange = true;
        }
        else
        {
            pendingChange = false;
            Changed?.Invoke();
        }
    }

    /// <summary>
    /// Moves the selection by a specified number of rows and columns.
    /// </summary>
    public CellRef Move(int rowOffset, int columnOffset)
    {
        var column = OffsetColumn(columnOffset);
        var row = OffsetRow(rowOffset);

        var cell = sheet.Clamp(new CellRef(row, column));

        return Select(cell);
    }

    private int OffsetRow(int rowOffset)
    {
        return rowOffset switch
        {
            > 0 => MergeEnd(Cell).Row + rowOffset,
            < 0 => MergeStart(Cell).Row + rowOffset,
            0 => Cell.Row + rowOffset
        };
    }

    private int OffsetColumn(int columnOffset)
    {
        return columnOffset switch
        {
            > 0 => MergeEnd(Cell).Column + columnOffset,
            < 0 => MergeStart(Cell).Column + columnOffset,
            0 => Cell.Column + columnOffset
        };
    }

    /// <summary>
    /// Cycles through the selection within the current range, moving by the specified row and column offsets.
    /// </summary>
    public CellRef Cycle(int rowOffset, int columnOffset)
    {
        if (Cell == CellRef.Invalid)
        {
            var address = new CellRef(0, 0);

            Select(address);

            return address;
        }

        if (Range.Collapsed || sheet.MergedCells.Contains(Range))
        {
            return Move(rowOffset, columnOffset);
        }
        else
        {
            var column = OffsetColumn(columnOffset);
            var row = OffsetRow(rowOffset);

            if (row < Range.Start.Row)
            {
                row = Range.End.Row;
                column += Math.Sign(rowOffset);
            }
            else if (row > Range.End.Row)
            {
                row = Range.Start.Row;
                column += Math.Sign(rowOffset);
            }

            if (column < Range.Start.Column)
            {
                column = Range.End.Column;
                row += Math.Sign(columnOffset);
            }
            else if (column > Range.End.Column)
            {
                column = Range.Start.Column;
                row += Math.Sign(columnOffset);
            }

            if (row < Range.Start.Row)
            {
                row = Range.End.Row;
            }
            else if (row > Range.End.Row)
            {
                row = Range.Start.Row;
            }

            var address = MergeStart(new CellRef(row, column));

            Update(address);

            return address;
        }
    }

    /// <summary>
    /// Selects a column in the spreadsheet, updating the active cell and range to cover the entire column.
    /// </summary>
    /// <param name="column"></param>
    public void Select(ColumnRef column)
    {
        Cell = new CellRef(0, column.Column);
        Range = new RangeRef(Cell, new CellRef(sheet.RowCount - 1, column.Column));

        TriggerChange();
    }

    /// <summary>
    /// Selects a row in the spreadsheet, updating the active cell and range to cover the entire row.
    /// </summary>
    public void Select(RowRef row)
    {
        Cell = new CellRef(row.Row, 0);
        Range = new RangeRef(Cell, new CellRef(row.Row, sheet.ColumnCount - 1));

        TriggerChange();
    }

    /// <summary>
    /// Checks if the specified address is within the active selection range.
    /// </summary>
    public bool IsActive(ColumnRef address) => Range.Contains(address);

    /// <summary>
    /// Checks if the specified address is within the active selection range.
    /// </summary>
    public bool IsActive(RowRef address) => Range.Contains(address);

    /// <summary>
    /// Checks if the specified cell address is within the active selection range.
    /// </summary>
    public bool IsSelected(RowRef address) => Range.End.Column == sheet.ColumnCount - 1 && Range.Start.Column == 0 && IsActive(address);

    /// <summary>
    /// Checks if the specified column address is within the active selection range.
    /// </summary>
    public bool IsSelected(ColumnRef address) => Range.End.Row == sheet.RowCount - 1 && Range.Start.Row == 0 && IsActive(address);

    /// <summary>
    /// Selects a cell address.
    /// </summary>
    public CellRef Select(CellRef address)
    {
        var mergedRange = sheet.MergedCells.GetMergedRange(address);

        if (mergedRange != RangeRef.Invalid)
        {
            Cell = mergedRange.Start;
            Range = mergedRange;
        }
        else
        {
            Cell = address;
            Range = RangeRef.FromCell(address);
        }

        TriggerChange();

        return Cell;
    }

    /// <summary>
    /// Selects a cell within a specified range, ensuring the cell is part of the range.
    /// </summary>
    public void Select(CellRef cell, RangeRef range)
    {
        if (!range.Contains(cell))
        {
            throw new ArgumentException("Cell must be within the specified range.", nameof(cell));
        }

        Cell = MergeStart(cell);

        range = ExpandRange(range);

        Range = new RangeRef(MergeStart(range.Start), MergeEnd(range.End));

        TriggerChange();
    }

    private RangeRef ExpandRange(RangeRef range)
    {
        var overlappingMerged = sheet.MergedCells.GetOverlappingRanges(range);
        foreach (var merged in overlappingMerged)
        {
            range = new RangeRef(
                new CellRef(
                    Math.Min(range.Start.Row, merged.Start.Row),
                    Math.Min(range.Start.Column, merged.Start.Column)
                ),
                new CellRef(
                    Math.Max(range.End.Row, merged.End.Row),
                    Math.Max(range.End.Column, merged.End.Column)
                )
            );
        }
        return range;
    }

    /// <summary>
    /// Selects a range of cells based on the provided RangeRef.
    /// </summary>
    /// <param name="range"></param>
    public void Select(RangeRef range) => Select(range.Start, range);

    /// <summary>
    /// Clears the current selection, resetting both the active cell and range to invalid states.
    /// </summary>
    public void Clear()
    {
        Cell = CellRef.Invalid;
        Range = RangeRef.Invalid;

        TriggerChange();
    }

    /// <summary>
    /// Merges the current selection with another cell address, expanding the selection range to include both addresses.
    /// </summary>
    /// <param name="address"></param>
    public void Merge(CellRef address)
    {
        var (start, end) = CellRef.Swap(Cell, address);
        var range = RangeRef.FromCells(MergeStart(start), MergeEnd(end));
        Range = ExpandRange(range);
        TriggerChange();
    }

    /// <summary>
    /// Extends the current selection by a specified number of rows and columns, adjusting the active cell and range accordingly.
    /// </summary>
    public CellRef Extend(int rowOffset, int columnOffset)
    {
        if (Cell == CellRef.Invalid || Range == RangeRef.Invalid)
        {
            return Cell;
        }

        var cell = Cell;
        var start = Range.Start;
        var end = Range.End;

        bool useStart;

        if (columnOffset > 0)
        {
            useStart = MergeEnd(cell).Column == end.Column;

            if (useStart)
            {
                start = MergeEnd(start);
            }
        }
        else if (columnOffset < 0)
        {
            useStart = cell.Column != start.Column;

            if (useStart == false)
            {
                end = MergeStart(end);
            }
        }
        else if (rowOffset > 0)
        {
            useStart = MergeEnd(cell).Row == end.Row;

            if (useStart)
            {
                start = MergeEnd(start);
            }
        }
        else
        {
            useStart = cell.Row != start.Row;
            if (useStart == false)
            {
                end = MergeStart(end);
            }
        }

        if (useStart)
        {
            cell = sheet.Clamp(new CellRef(start.Row + rowOffset, start.Column + columnOffset));

            start = cell;
        }
        else
        {
            cell = sheet.Clamp(new CellRef(end.Row + rowOffset, end.Column + columnOffset));

            end = cell;
        }

        (start, end) = CellRef.Swap(start, end);

        Range = new RangeRef(MergeStart(start), MergeEnd(end));

        TriggerChange();

        return cell;
    }

    private CellRef MergeStart(CellRef address)
    {
        var mergedRange = sheet.MergedCells.GetMergedRange(address);

        if (mergedRange != RangeRef.Invalid)
        {
            return mergedRange.Start;
        }

        return address;
    }

    private CellRef MergeEnd(CellRef address)
    {
        var mergedRange = sheet.MergedCells.GetMergedRange(address);

        if (mergedRange != RangeRef.Invalid)
        {
            return mergedRange.End;
        }

        return address;
    }
}