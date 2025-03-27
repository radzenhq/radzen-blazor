namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents an inline image element: <c>![Alt text](/path/to/img.jpg "Optional title")</c>
/// </summary>
public class Image : InlineContainer
{
    /// <summary>
    /// Gets or sets the destination (URL) of the image.
    /// </summary>
    public string Destination { get; set; }

    /// <summary>
    /// Gets or sets the alternative text of the image.
    /// </summary>
    public string Title { get; set; }

    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitImage(this);
    }
}