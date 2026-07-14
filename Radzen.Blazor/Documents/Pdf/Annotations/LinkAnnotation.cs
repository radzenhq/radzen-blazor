using System;
using System.Text;

namespace Radzen.Documents.Pdf;

/// <summary>Represents a link to a URI, named destination or page.</summary>
/// <param name="bounds">The clickable bounds.</param>
public sealed class LinkAnnotation(Rect bounds) : Annotation(bounds)
{
    /// <summary>Gets or sets the target URI.</summary>
    public Uri? Uri { get; set; }

    /// <summary>Gets or sets the target named destination.</summary>
    public string? Destination { get; set; }

    /// <summary>Gets or sets the zero-based target page index.</summary>
    public int? TargetPageIndex { get; set; }

    internal bool DestinationIsName { get; set; }

    internal override string Subtype => "Link";

    internal override void AppendState(StringBuilder value)
        => value.Append('|').Append(Uri?.OriginalString).Append('|').Append(Destination).Append('|').Append(TargetPageIndex);
}
