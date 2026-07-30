using System;
using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents;


/// <summary>
/// An ordered collection of the rows in a table. A new row materializes one cell per existing
/// column; a row re-added after removal keeps its cells and must still have exactly one per column.
/// A row belongs to exactly one table: adding a row that already has a parent throws.
/// </summary>
public sealed class RowCollection : IReadOnlyList<Row>
{
    private readonly TrackedList<Row> items = [];
    private readonly Table table;

    internal RowCollection(Table table) => this.table = table;

    /// <inheritdoc/>
    public int Count => items.Count;

    /// <inheritdoc/>
    public Row this[int index] => items[index];

    internal bool StructureChanged => items.StructureChanged;

    internal void AcceptStructure() => items.AcceptStructure();

    /// <summary>
    /// Appends a new row with one cell per existing column.
    /// </summary>
    /// <returns>The newly created row.</returns>
    public Row Add() => Add(Create());

    /// <summary>
    /// Appends an existing row, typically one previously removed from this table.
    /// </summary>
    /// <param name="row">The row to append.</param>
    /// <returns>The same <paramref name="row"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="row"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="row"/> already belongs to another table, appending it would make the document
    /// tree cyclic, or its cell count does not match the table's column count.
    /// </exception>
    public Row Add(Row row)
    {
        ArgumentNullException.ThrowIfNull(row);
        Own(row);
        items.Add(row);
        return row;
    }

    /// <summary>
    /// Inserts a new row with one cell per existing column at the specified position.
    /// </summary>
    /// <param name="index">The zero-based position to insert at, from 0 to <see cref="Count"/>.</param>
    /// <returns>The newly created row.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public Row Insert(int index) => Insert(index, Create());

    /// <summary>
    /// Inserts an existing row at the specified position.
    /// </summary>
    /// <param name="index">The zero-based position to insert at, from 0 to <see cref="Count"/>.</param>
    /// <param name="row">The row to insert.</param>
    /// <returns>The same <paramref name="row"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="row"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="row"/> already belongs to another table, inserting it would make the document
    /// tree cyclic, or its cell count does not match the table's column count.
    /// </exception>
    public Row Insert(int index, Row row)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, items.Count);
        ArgumentNullException.ThrowIfNull(row);
        Own(row);
        items.Insert(index, row);
        return row;
    }

    /// <summary>
    /// Removes the row at the specified index, detaching it so it may be added back later.
    /// </summary>
    /// <param name="index">The zero-based index of the row to remove.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public void RemoveAt(int index)
    {
        var row = items[index];
        items.RemoveAt(index);
        ContentTree.Detach(row);
    }

    /// <summary>
    /// Removes the specified row, detaching it so it may be added back later.
    /// </summary>
    /// <param name="row">The row to remove.</param>
    /// <returns><see langword="true"/> if the row was removed; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="row"/> is <see langword="null"/>.</exception>
    public bool Remove(Row row)
    {
        ArgumentNullException.ThrowIfNull(row);

        if (!items.Remove(row))
        {
            return false;
        }

        ContentTree.Detach(row);
        return true;
    }

    /// <summary>
    /// Moves the row at <paramref name="fromIndex"/> to <paramref name="toIndex"/>, shifting the
    /// rows in between.
    /// </summary>
    /// <param name="fromIndex">The zero-based index of the row to move.</param>
    /// <param name="toIndex">The zero-based index the row ends up at.</param>
    /// <exception cref="ArgumentOutOfRangeException">Either index is out of range.</exception>
    public void Move(int fromIndex, int toIndex) => items.Move(fromIndex, toIndex);

    /// <summary>
    /// Removes every row, detaching each one so it may be added back later. The columns are kept.
    /// </summary>
    public void Clear()
    {
        foreach (var row in items)
        {
            ContentTree.Detach(row);
        }

        items.Clear();
    }

    private Row Create()
    {
        var row = new Row();
        for (var i = 0; i < table.Columns.Count; i++)
        {
            row.Cells.AddCell();
        }

        return row;
    }

    private void Own(Row row)
    {
        ContentTree.Attach(row, this);

        if (row.Cells.Count != table.Columns.Count)
        {
            ContentTree.Detach(row);
            throw new InvalidOperationException(
                $"The row has {row.Cells.Count} cells but the table has {table.Columns.Count} columns; "
                + "a row must hold exactly one cell per column.");
        }
    }

    /// <inheritdoc/>
    public IEnumerator<Row> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
