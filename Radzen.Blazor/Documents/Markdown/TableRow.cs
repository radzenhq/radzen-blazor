using System;
using System.Collections.Generic;

namespace Radzen.Documents.Markdown;

/// <summary>
/// Represents a table row in a Markdown table.
/// </summary>
public class TableRow : INode
{
    private readonly List<TableCell> children = [];

    /// <summary>
    /// Gets the cells of the table row.
    /// </summary>
    public IReadOnlyList<TableCell> Cells => children;

    /// <summary>
    /// Adds a cell to the table row.
    /// </summary>
    /// <param name="value">The value of the cell.</param>
    /// <param name="alignment">The alignment of the cell.</param>
    public void Add(string value, TableCellAlignment alignment = TableCellAlignment.None)
    {
        var cell = new TableCell(value, alignment);
        children.Add(cell);
    }

    internal void Add(string value, TableCellAlignment alignment, ContentMap content)
    {
        var cell = new TableCell(value, alignment);

        cell.Content.Append(content.Segments);

        children.Add(cell);
    }

    internal void Insert(int index, TableCell cell) => children.Insert(index, cell);

    internal void RemoveAt(int index) => children.RemoveAt(index);

    /// <inheritdoc />
    public virtual void Accept(INodeVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.VisitTableRow(this);
    }
}

