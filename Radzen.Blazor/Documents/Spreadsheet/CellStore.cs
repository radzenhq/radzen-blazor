using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Radzen.Documents.Spreadsheet;

/// <summary>
/// Represents a store for spreadsheet cells, allowing access and modification of cell values.
/// </summary>
/// <param name="sheet"></param>
[SuppressMessage("Design", "CA1043:Use Integral Or String Argument For Indexers", Justification = "CellRef indexer is intentional for spreadsheet access.")]
public class CellStore(Worksheet sheet)
{
    /// <summary>
    /// Gets the sheet that owns this cell store.
    /// </summary>
    public Worksheet Worksheet { get; } = sheet;

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
                this[row, column] = cell = new(Worksheet, new CellRef(row, column));
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
        return row >= 0 && row < Worksheet.RowCount && column >= 0 && column < Worksheet.ColumnCount;
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

    internal IEnumerable<Cell> GetPopulatedCells() => data.Values;

    internal int Compact()
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

    internal int PopulatedCount => data.Count;

    internal bool HasCell(int row, int column) => data.ContainsKey((row, column));

    private static void UpdateCellAddress((int row, int column) oldKey, (int row, int column) newKey, Cell cell)
    {
        if (oldKey != newKey)
        {
            cell.Address = new CellRef(newKey.row, newKey.column);
        }
    }

    internal void ShiftRowsUp(int deletedRow) =>
        DictionaryShift.Remap<(int row, int column), Cell>(data, k =>
            k.row < deletedRow ? k :
            k.row == deletedRow ? null :
            (k.row - 1, k.column),
            UpdateCellAddress);

    internal void ShiftRowsDown(int fromRow, int count) =>
        DictionaryShift.Remap<(int row, int column), Cell>(data, k =>
            k.row < fromRow ? k : (k.row + count, k.column),
            UpdateCellAddress);

    internal void ShiftColumnsLeft(int deletedColumn) =>
        DictionaryShift.Remap<(int row, int column), Cell>(data, k =>
            k.column < deletedColumn ? k :
            k.column == deletedColumn ? null :
            (k.row, k.column - 1),
            UpdateCellAddress);

    internal void ShiftColumnsRight(int fromColumn, int count) =>
        DictionaryShift.Remap<(int row, int column), Cell>(data, k =>
            k.column < fromColumn ? k : (k.row, k.column + count),
            UpdateCellAddress);

    private readonly Dictionary<RangeRef, string> customTypes = [];

    /// <summary>
    /// Sets a custom cell type for the specified cell.
    /// </summary>
    /// <param name="cell">The cell reference.</param>
    /// <param name="type">The custom type name, or null to remove the custom type.</param>
    public void SetCustomType(CellRef cell, string? type)
    {
        SetCustomType(cell.ToRange(), type);
    }

    /// <summary>
    /// Sets a custom cell type for the specified range.
    /// </summary>
    /// <param name="range">The range of cells.</param>
    /// <param name="type">The custom type name, or null to remove the custom type.</param>
    public void SetCustomType(RangeRef range, string? type)
    {
        if (type is null)
        {
            customTypes.Remove(range);
        }
        else
        {
            customTypes[range] = type;
        }
        Worksheet.OnChromeChanged();
    }

    /// <summary>
    /// Gets the custom cell type for the specified cell, or null if no custom type is set.
    /// </summary>
    /// <param name="row">The row index of the cell.</param>
    /// <param name="column">The column index of the cell.</param>
    /// <returns>The custom type name, or null if no custom type is set.</returns>
    public string? GetCustomType(int row, int column)
    {
        foreach (var kvp in customTypes)
        {
            if (kvp.Key.Contains(row, column))
            {
                return kvp.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Ensures that the specified row and column indices are within the bounds of the sheet.
    /// </summary>
    /// <param name="row">The row index to check.</param>
    /// <param name="column">The column index to check.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the row or column index is out of range.</exception>
    protected void EnsureWithinBounds(int row, int column)
    {
        if (row < 0 || row >= Worksheet.RowCount)
        {
            throw new ArgumentOutOfRangeException(nameof(row), "Row index is out of range.");
        }

        if (column < 0 || column >= Worksheet.ColumnCount)
        {
            throw new ArgumentOutOfRangeException(nameof(column), "Column index is out of range.");
        }
    }
}