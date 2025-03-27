namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents a line break node. Line breaks are usually empty lines and are used to separate paragraphs.
/// </summary>
public class LineBreak : Inline
{
    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitLineBreak(this);
    }
}