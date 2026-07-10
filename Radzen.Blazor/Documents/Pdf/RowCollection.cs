using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// An ordered, read-only view of the rows in a table. Adding a row materializes one cell per existing
/// column.
/// </summary>
public class RowCollection : IReadOnlyList<Row>
{
    private readonly List<Row> items = new();
    private readonly Table table;

    internal RowCollection(Table table) => this.table = table;

    /// <inheritdoc/>
    public int Count => items.Count;

    /// <inheritdoc/>
    public Row this[int index] => items[index];

    /// <summary>
    /// Appends a new row with one cell per existing column.
    /// </summary>
    /// <returns>The newly created row.</returns>
    public Row Add()
    {
        var row = new Row();
        items.Add(row);
        for (var i = 0; i < table.Columns.Count; i++)
        {
            row.Cells.AddCell();
        }

        return row;
    }

    /// <inheritdoc/>
    public IEnumerator<Row> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
