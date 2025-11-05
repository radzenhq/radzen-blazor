using System.Collections.Generic;

namespace Radzen.Blazor.Markdown;

#nullable enable

/// <summary>
/// Options for configuring the Blazor Markdown renderer.
/// </summary>
internal class BlazorMarkdownRendererOptions
{
    /// <summary>
    /// Gets or sets the maximum heading depth for auto-linking.
    /// </summary>
    public int AutoLinkHeadingDepth { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether HTML is allowed in the markdown.
    /// </summary>
    public bool AllowHtml { get; set; }

    /// <summary>
    /// Gets or sets the list of allowed HTML tags.
    /// </summary>
    public IEnumerable<string>? AllowedHtmlTags { get; set; }

    /// <summary>
    /// Gets or sets the list of allowed HTML attributes.
    /// </summary>
    public IEnumerable<string>? AllowedHtmlAttributes { get; set; }
}

