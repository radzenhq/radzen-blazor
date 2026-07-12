using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


/// <summary>
/// An ordered, read-only view of the rows in a table. Adding a row materializes one cell per existing
/// column.
/// </summary>
public class RowCollection : IReadOnlyList<Row>
{
    private readonly List<Row> items = [];
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

    /// <summary>
    /// Removes the row at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the row to remove.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="index"/> is out of range.</exception>
    public void RemoveAt(int index) => items.RemoveAt(index);

    /// <summary>
    /// Removes the specified row.
    /// </summary>
    /// <param name="row">The row to remove.</param>
    /// <returns><see langword="true"/> if the row was removed; otherwise <see langword="false"/>.</returns>
    public bool Remove(Row row) => items.Remove(row);

    /// <inheritdoc/>
    public IEnumerator<Row> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
