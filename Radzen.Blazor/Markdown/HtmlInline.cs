namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents an inline HTML element.
/// </summary>
public class HtmlInline : Inline
{
    /// <summary>
    /// Gets or sets the HTML element value.
    /// </summary>
    public string Value { get; set; }


    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        visitor.VisitHtmlInline(this);
    }
}