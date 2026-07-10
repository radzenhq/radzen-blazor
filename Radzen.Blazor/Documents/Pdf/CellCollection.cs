using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// A read-only view of the cells in a row. Cells are materialized automatically as columns are added.
/// </summary>
public class CellCollection : IReadOnlyList<Cell>
{
    private readonly List<Cell> items = new();

    /// <inheritdoc/>
    public int Count => items.Count;

    /// <inheritdoc/>
    public Cell this[int index] => items[index];

    internal Cell AddCell()
    {
        var cell = new Cell();
        items.Add(cell);
        return cell;
    }

    /// <inheritdoc/>
    public IEnumerator<Cell> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
