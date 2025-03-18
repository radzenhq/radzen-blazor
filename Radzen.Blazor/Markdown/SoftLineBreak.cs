namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents a soft line break node. Soft line breaks are usually used to separate lines in a paragraph.
/// </summary>
public class SoftLineBreak : Inline
{
    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitSoftLineBreak(this);
    }
}