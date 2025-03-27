namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents a link element: <c>[Link text](/path/to/page "Optional title")</c>
/// </summary>
public class Link : InlineContainer
{
    /// <summary>
    /// Gets or sets the destination (URL) of the link.
    /// </summary>
    public string Destination { get; set; }

    /// <summary>
    /// Gets or sets the link title.
    /// </summary>
    public string Title { get; set; }

    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitLink(this);
    }
}