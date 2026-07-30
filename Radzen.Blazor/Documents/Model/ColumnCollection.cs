using System;
using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents;


/// <summary>
/// An ordered collection of the columns in a table. The columns and each row's cells are kept in
/// step: every mutation here adds, inserts, moves or removes the cell at the same position in
/// every existing row. A column belongs to exactly one table: adding a column that already has a
/// parent throws, while one removed from this table may be added back.
/// </summary>
public sealed class ColumnCollection : IReadOnlyList<Column>
{
    private readonly TrackedList<Column> items = [];
    private readonly Table table;

    internal ColumnCollection(Table table) => this.table = table;

    /// <inheritdoc/>
    public int Count => items.Count;

    /// <inheritdoc/>
    public Column this[int index] => items[index];

    internal bool StructureChanged => items.StructureChanged;

    internal void AcceptStructure() => items.AcceptStructure();

    /// <summary>
    /// Appends an automatically sized column, adding a cell to the end of every existing row.
    /// </summary>
    /// <returns>The newly created column.</returns>
    public Column Add() => Add(new Column());

    /// <summary>
    /// Appends a column with a fixed width, adding a cell to the end of every existing row.
    /// </summary>
    /// <param name="width">The column width.</param>
    /// <returns>The newly created column.</returns>
    public Column Add(Unit width) => Add(new Column { Width = width });

    /// <summary>
    /// Inserts an automatically sized column at the specified position, inserting a cell at the
    /// same position in every existing row.
    /// </summary>
    /// <param name="index">The zero-based position to insert at, from 0 to <see cref="Count"/>.</param>
    /// <returns>The newly created column.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public Column Insert(int index) => Insert(index, new Column());

    /// <summary>
    /// Inserts a column with a fixed width at the specified position, inserting a cell at the same
    /// position in every existing row.
    /// </summary>
    /// <param name="index">The zero-based position to insert at, from 0 to <see cref="Count"/>.</param>
    /// <param name="width">The column width.</param>
    /// <returns>The newly created column.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public Column Insert(int index, Unit width) => Insert(index, new Column { Width = width });

    /// <summary>
    /// Removes the specified column together with the matching cell of every row.
    /// </summary>
    /// <param name="column">The column to remove.</param>
    /// <returns><see langword="true"/> if the column was in the collection; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="column"/> is <see langword="null"/>.</exception>
    public bool Remove(Column column)
    {
        ArgumentNullException.ThrowIfNull(column);

        var index = items.IndexOf(column);
        if (index < 0)
        {
            return false;
        }

        RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Removes the column at the specified position together with the matching cell of every row.
    /// </summary>
    /// <param name="index">The zero-based index of the column to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public void RemoveAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, items.Count);

        var column = items[index];
        items.RemoveAt(index);
        ContentTree.Detach(column);
        foreach (var row in table.Rows)
        {
            row.Cells.RemoveCellAt(index);
        }
    }

    /// <summary>
    /// Moves the column at <paramref name="fromIndex"/> to <paramref name="toIndex"/>, moving the
    /// matching cell of every row with it.
    /// </summary>
    /// <param name="fromIndex">The zero-based index of the column to move.</param>
    /// <param name="toIndex">The zero-based index the column ends up at.</param>
    /// <exception cref="ArgumentOutOfRangeException">Either index is out of range.</exception>
    public void Move(int fromIndex, int toIndex)
    {
        items.Move(fromIndex, toIndex);
        foreach (var row in table.Rows)
        {
            row.Cells.MoveCell(fromIndex, toIndex);
        }
    }

    /// <summary>
    /// Removes every column and, with them, every cell of every row. The rows themselves are kept.
    /// </summary>
    public void Clear()
    {
        foreach (var column in items)
        {
            ContentTree.Detach(column);
        }

        items.Clear();
        foreach (var row in table.Rows)
        {
            row.Cells.ClearCells();
        }
    }

    /// <summary>
    /// Appends an existing column, typically one previously removed from this table, adding a cell
    /// to the end of every existing row.
    /// </summary>
    /// <param name="column">The column to append.</param>
    /// <returns>The same <paramref name="column"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="column"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="column"/> already belongs to another table.</exception>
    public Column Add(Column column)
    {
        ContentTree.Attach(column, this);
        items.Add(column);
        foreach (var row in table.Rows)
        {
            row.Cells.AddCell();
        }

        return column;
    }

    /// <summary>
    /// Inserts an existing column at the specified position, inserting a cell at the same position
    /// in every existing row.
    /// </summary>
    /// <param name="index">The zero-based position to insert at, from 0 to <see cref="Count"/>.</param>
    /// <param name="column">The column to insert.</param>
    /// <returns>The same <paramref name="column"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="column"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="column"/> already belongs to another table.</exception>
    public Column Insert(int index, Column column)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, items.Count);
        ContentTree.Attach(column, this);

        items.Insert(index, column);
        foreach (var row in table.Rows)
        {
            row.Cells.InsertCell(index);
        }

        return column;
    }

    /// <inheritdoc/>
    public IEnumerator<Column> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
