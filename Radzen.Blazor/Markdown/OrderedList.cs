namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents an ordered list: <c>1. item</c>.
/// </summary>
public class OrderedList : List
{
    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitOrderedList(this);
    }

    /// <summary>
    /// Gets or sets the start number of the ordered list.
    /// </summary>
    public int Start { get; set; } 
}