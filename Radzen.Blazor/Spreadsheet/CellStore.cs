using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Represents a store for spreadsheet cells, allowing access and modification of cell values.
/// </summary>
/// <param name="sheet"></param>
[SuppressMessage("Design", "CA1043:Use Integral Or String Argument For Indexers", Justification = "CellRef indexer is intentional for spreadsheet access.")]
public class CellStore(Sheet sheet)
{
    /// <summary>
    /// Gets the sheet that owns this cell store.
    /// </summary>
    public Sheet Sheet { get; } = sheet;

    /// <summary>
    /// Stores the cells in a dictionary, where the key is a tuple of (row, column).
    /// </summary>
    protected readonly Dictionary<(int row, int column), Cell> data = [];

    /// <summary>
    /// Gets a cell at the specified row and column.
    /// If the cell does not exist, it is created and added to the store.
    /// </summary>
    /// <param name="row">The row index of the cell.</param>
    /// <param name="column">The column index of the cell.</param>
    /// <returns>The cell at the specified row and column.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the row or column index is out of bounds.</exception>
    /// <remarks>
    /// The row and column indices are zero-based, so the first cell is at (0, 0).
    /// The method ensures that the specified row and column are within the bounds of the sheet's dimensions.
    /// If the cell does not exist in the store, it creates a new cell with the specified row and column,
    /// initializes it with the sheet reference, and adds it to the store.
    /// </remarks>
    public virtual Cell this[int row, int column]
    {
        get
        {
            EnsureWithinBounds(row, column);

            if (!data.TryGetValue((row, column), out var cell))
            {
                this[row, column] = cell = new(Sheet, new CellRef(row, column));
            }

            return cell;
        }

        set
        {
            EnsureWithinBounds(row, column);

            data[(row, column)] = value;
        }
    }

    /// <summary>
    /// Gets or sets a cell at the specified address using a CellRef object.
    /// </summary>

    public Cell this[CellRef address]
    {
        get => this[address.Row, address.Column];
        set => this[address.Row, address.Column] = value;
    }

    /// <summary>
    /// Gets or sets a cell at the specified address using a string in A1 notation.
    /// </summary>
    public Cell this[string address]
    {
        get => this[CellRef.Parse(address)];
        set => this[CellRef.Parse(address)] = value;
    }

    internal List<Cell> GetRow(int row, int start, int end)
    {
        var cells = new List<Cell>(end - start + 1);

        for (var column = start; column <= end; column++)
        {
            cells.Add(this[row, column].Clone());
        }

        return cells;
    }

    private bool InBounds(int row, int column)
    {
        return row >= 0 && row < Sheet.RowCount && column >= 0 && column < Sheet.ColumnCount;
    }

    /// <summary>
    /// Attempts to get a cell at the specified row and column.
    /// Returns true only if the cell has been populated. Does not create new cells.
    /// </summary>
    /// <param name="row">The row index of the cell.</param>
    /// <param name="column">The column index of the cell.</param>
    /// <param name="cell">The cell at the specified row and column if it exists; otherwise, null.</param>
    /// <returns>True if the cell exists in the store; otherwise, false.</returns>
    public bool TryGet(int row, int column, out Cell cell)
    {
        if (InBounds(row, column) && data.TryGetValue((row, column), out cell!))
        {
            return true;
        }

        cell = null!;

        return false;
    }

    /// <summary>
    /// Returns all populated cells in the store without creating new cells.
    /// </summary>
    internal IEnumerable<Cell> GetPopulatedCells() => data.Values;

    /// <summary>
    /// Removes all empty cells from the store, freeing memory.
    /// A cell is empty when it has no value, formula, format, or hyperlink.
    /// </summary>
    /// <returns>The number of cells removed.</returns>
    public int Compact()
    {
        var keysToRemove = new List<(int row, int column)>();

        foreach (var kvp in data)
        {
            if (kvp.Value.IsEmpty)
            {
                keysToRemove.Add(kvp.Key);
            }
        }

        foreach (var key in keysToRemove)
        {
            data.Remove(key);
        }

        return keysToRemove.Count;
    }

    /// <summary>
    /// Returns the number of populated cells in the store.
    /// </summary>
    internal int PopulatedCount => data.Count;

    /// <summary>
    /// Checks if a cell exists at the specified position without creating it.
    /// </summary>
    internal bool HasCell(int row, int column) => data.ContainsKey((row, column));

    /// <summary>
    /// Removes all cells at the specified row and shifts cells from higher rows up by one.
    /// Only touches populated cells, making this O(populated) instead of O(rows × columns).
    /// </summary>
    internal void ShiftRowsUp(int deletedRow)
    {
        var toRemove = new List<(int row, int column)>();
        var toRekey = new List<((int row, int column) key, Cell cell)>();

        foreach (var kvp in data)
        {
            if (kvp.Key.row == deletedRow)
            {
                toRemove.Add(kvp.Key);
            }
            else if (kvp.Key.row > deletedRow)
            {
                toRekey.Add((kvp.Key, kvp.Value));
            }
        }

        foreach (var key in toRemove)
        {
            data.Remove(key);
        }

        foreach (var (key, cell) in toRekey)
        {
            data.Remove(key);
            var newRow = key.row - 1;
            data[(newRow, key.column)] = cell;
            cell.Address = new CellRef(newRow, key.column);
        }
    }

    /// <summary>
    /// Shifts all cells at or after the specified row down by the given count.
    /// Only touches populated cells, making this O(populated) instead of O(rows × columns).
    /// </summary>
    internal void ShiftRowsDown(int fromRow, int count)
    {
        var toRekey = new List<((int row, int column) key, Cell cell)>();

        foreach (var kvp in data)
        {
            if (kvp.Key.row >= fromRow)
            {
                toRekey.Add((kvp.Key, kvp.Value));
            }
        }

        // Remove all affected entries first to avoid key conflicts
        foreach (var (key, _) in toRekey)
        {
            data.Remove(key);
        }

        // Add back at new positions
        foreach (var (key, cell) in toRekey)
        {
            var newRow = key.row + count;
            data[(newRow, key.column)] = cell;
            cell.Address = new CellRef(newRow, key.column);
        }
    }

    /// <summary>
    /// Removes all cells at the specified column and shifts cells from higher columns left by one.
    /// Only touches populated cells, making this O(populated) instead of O(rows × columns).
    /// </summary>
    internal void ShiftColumnsLeft(int deletedColumn)
    {
        var toRemove = new List<(int row, int column)>();
        var toRekey = new List<((int row, int column) key, Cell cell)>();

        foreach (var kvp in data)
        {
            if (kvp.Key.column == deletedColumn)
            {
                toRemove.Add(kvp.Key);
            }
            else if (kvp.Key.column > deletedColumn)
            {
                toRekey.Add((kvp.Key, kvp.Value));
            }
        }

        foreach (var key in toRemove)
        {
            data.Remove(key);
        }

        foreach (var (key, cell) in toRekey)
        {
            data.Remove(key);
            var newCol = key.column - 1;
            data[(key.row, newCol)] = cell;
            cell.Address = new CellRef(key.row, newCol);
        }
    }

    /// <summary>
    /// Shifts all cells at or after the specified column right by the given count.
    /// Only touches populated cells, making this O(populated) instead of O(rows × columns).
    /// </summary>
    internal void ShiftColumnsRight(int fromColumn, int count)
    {
        var toRekey = new List<((int row, int column) key, Cell cell)>();

        foreach (var kvp in data)
        {
            if (kvp.Key.column >= fromColumn)
            {
                toRekey.Add((kvp.Key, kvp.Value));
            }
        }

        // Remove all affected entries first to avoid key conflicts
        foreach (var (key, _) in toRekey)
        {
            data.Remove(key);
        }

        // Add back at new positions
        foreach (var (key, cell) in toRekey)
        {
            var newCol = key.column + count;
            data[(key.row, newCol)] = cell;
            cell.Address = new CellRef(key.row, newCol);
        }
    }

    /// <summary>
    /// Ensures that the specified row and column indices are within the bounds of the sheet.
    /// </summary>
    /// <param name="row">The row index to check.</param>
    /// <param name="column">The column index to check.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the row or column index is out of range.</exception>
    protected void EnsureWithinBounds(int row, int column)
    {
        if (row < 0 || row >= Sheet.RowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(row), "Row index is out of range.");
        }

        if (column < 0 || column >= Sheet.ColumnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(column), "Column index is out of range.");
        }
    }
}