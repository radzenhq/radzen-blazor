using System.Collections;
using System.Collections.Generic;
using Radzen.Documents.Core;

namespace Radzen.Documents;


/// <summary>
/// A read-only view of the cells in a row. Cells are structural: a row always holds exactly one
/// cell per table column, so the collection is mutated only by adding, inserting, moving or
/// removing columns on <see cref="Table.Columns"/>, which keeps every row in step.
/// </summary>
public sealed class CellCollection : IReadOnlyList<Cell>
{
    internal CellCollection()
    {
    }

    private readonly TrackedList<Cell> items = [];

    /// <inheritdoc/>
    public int Count => items.Count;

    /// <inheritdoc/>
    public Cell this[int index] => items[index];

    internal bool StructureChanged => items.StructureChanged;

    internal void AcceptStructure() => items.AcceptStructure();

    internal Cell AddCell()
    {
        var cell = new Cell();
        items.Add(cell);
        return cell;
    }

    internal Cell InsertCell(int index)
    {
        var cell = new Cell();
        items.Insert(index, cell);
        return cell;
    }

    internal void RemoveCellAt(int index) => items.RemoveAt(index);

    internal void MoveCell(int fromIndex, int toIndex) => items.Move(fromIndex, toIndex);

    internal void ClearCells() => items.Clear();

    /// <inheritdoc/>
    public IEnumerator<Cell> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
