namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents a strong node: <c>**strong**</c>.
/// </summary>
public class Strong : InlineContainer
{
    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitStrong(this);
    }
}