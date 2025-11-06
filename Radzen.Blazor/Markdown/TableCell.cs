using System.Collections.Generic;

namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents a table cell in a Markdown table.
/// </summary>
public class TableCell : INode, IBlockInlineContainer
{
    private readonly List<Inline> children = [];

    /// <summary>
    /// Gets the alignment of the table cell.
    /// </summary>
    public TableCellAlignment Alignment { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TableCell"/> class.
    /// </summary>
    /// <param name="value">The value of the cell.</param>
    /// <param name="alignment">The alignment of the cell.</param>
    public TableCell(string value, TableCellAlignment alignment = TableCellAlignment.None)
    {
        Value = value;
        Alignment = alignment;
    }

    /// <summary>
    /// Gets or sets the inline content of the cell.
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Gets the children of the table cell.
    /// </summary>
    public IReadOnlyList<Inline> Children => children;

    /// <summary>
    /// Appends a child to table cell.
    /// </summary>
    /// <param name="node">The inline node to add.</param>
    public void Add(Inline node)
    {
        children.Add(node);
    }

    /// <inheritdoc />
    public void Accept(INodeVisitor visitor)
    {
        visitor.VisitTableCell(this);
    }
}

