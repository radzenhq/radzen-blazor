using System;
using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

/// <summary>
/// Represents a store for spreadsheet cells, allowing access and modification of cell values.
/// </summary>
/// <param name="sheet"></param>
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
    /// </summary>
    /// <param name="row">The row index of the cell.</param>
    /// <param name="column">The column index of the cell.</param>
    /// <param name="cell">The cell at the specified row and column if it exists; otherwise, null.</param>
    /// <returns>True if the cell exists; otherwise, false.</returns>
    public bool TryGet(int row, int column, out Cell cell)
    {
        if (InBounds(row, column))
        {
            cell = this[row, column];
            return true;
        }

        cell = null!;

        return false;
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