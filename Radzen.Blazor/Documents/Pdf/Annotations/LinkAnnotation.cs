using System;

namespace Radzen.Documents.Pdf;

/// <summary>Represents a link to a URI, named destination or page.</summary>
/// <param name="bounds">The clickable bounds.</param>
public sealed class LinkAnnotation(PdfRect bounds) : Annotation(bounds)
{
    private Uri? uri;
    private string? destination;
    private int? targetPageIndex;
    private bool destinationIsName;

    /// <summary>Gets or sets the target URI.</summary>
    public Uri? Uri
    {
        get => uri;
        set => Set(ref uri, value);
    }

    /// <summary>Gets or sets the target named destination.</summary>
    public string? Destination
    {
        get => destination;
        set => Set(ref destination, value);
    }

    /// <summary>Gets or sets the zero-based target page index.</summary>
    public int? TargetPageIndex
    {
        get => targetPageIndex;
        set => Set(ref targetPageIndex, value);
    }

    internal bool DestinationIsName
    {
        get => destinationIsName;
        set => Set(ref destinationIsName, value);
    }

    internal override string Subtype => "Link";
}
