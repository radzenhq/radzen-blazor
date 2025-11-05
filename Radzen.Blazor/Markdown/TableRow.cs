using System.Collections.Generic;

namespace Radzen.Blazor.Markdown;

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

    /// <inheritdoc />
    public virtual void Accept(INodeVisitor visitor)
    {
        visitor.VisitTableRow(this);
    }
}

