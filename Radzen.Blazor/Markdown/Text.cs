namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents a text node.
/// </summary>
public class Text(string value) : Inline
{
    /// <summary>
    /// Gets or sets the text value.
    /// </summary>
    public string Value { get; set; } = value;

    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitText(this);
    }
}