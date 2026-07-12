using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


/// <summary>
/// An ordered, read-only view of the columns in a table. Adding a column retrofits a cell to every
/// existing row.
/// </summary>
public class ColumnCollection : IReadOnlyList<Column>
{
    private readonly List<Column> items = [];
    private readonly Table table;

    internal ColumnCollection(Table table) => this.table = table;

    /// <inheritdoc/>
    public int Count => items.Count;

    /// <inheritdoc/>
    public Column this[int index] => items[index];

    /// <summary>
    /// Appends an automatically sized column.
    /// </summary>
    /// <returns>The newly created column.</returns>
    public Column Add() => Add(new Column());

    /// <summary>
    /// Appends a column with a fixed width.
    /// </summary>
    /// <param name="width">The column width.</param>
    /// <returns>The newly created column.</returns>
    public Column Add(Unit width) => Add(new Column { Width = width });

    private Column Add(Column column)
    {
        items.Add(column);
        foreach (var row in table.Rows)
        {
            row.Cells.AddCell();
        }

        return column;
    }

    /// <inheritdoc/>
    public IEnumerator<Column> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
