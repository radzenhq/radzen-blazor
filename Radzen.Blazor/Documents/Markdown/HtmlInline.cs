using System;
using System.Text.RegularExpressions;

namespace Radzen.Documents.Markdown;

/// <summary>
/// Represents an inline HTML element.
/// </summary>
public class HtmlInline : Inline
{
    /// <summary>
    /// Gets or sets the HTML element value.
    /// </summary>
    public string? Value { get; set; }

    internal Match? Tag { get; init; }


    /// <inheritdoc />
    public override void Accept(INodeVisitor visitor)
    {
        ArgumentNullException.ThrowIfNull(visitor);
        visitor.VisitHtmlInline(this);
    }
}