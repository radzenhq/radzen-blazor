namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents a table header row in a Markdown table.
/// </summary>
public class TableHeaderRow : TableRow
{
    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitTableHeaderRow(this);
    }
}

