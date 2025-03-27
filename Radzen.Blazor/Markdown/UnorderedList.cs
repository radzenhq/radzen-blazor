namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents an unordered list: <c>- item</c>.
/// </summary>
public class UnorderedList : List
{
    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitUnorderedList(this);
    }
}