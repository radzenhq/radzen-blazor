using System.Collections.Generic;
using Radzen.Documents.Core;

namespace Radzen.Documents.Pdf;

/// <summary>Represents a sticky-note text annotation.</summary>
/// <param name="bounds">The annotation bounds.</param>
public sealed class TextAnnotation(PdfRect bounds) : Annotation(bounds)
{
    private bool open;
    private string icon = "Note";

    /// <summary>Gets or sets whether the note is initially open.</summary>
    public bool Open
    {
        get => open;
        set => Set(ref open, value);
    }

    /// <summary>Gets or sets the standard note icon name.</summary>
    public string Icon
    {
        get => icon;
        set => Set(ref icon, value);
    }

    internal override string Subtype => "Text";
}

/// <summary>Base class for text markup annotations.</summary>
public abstract class MarkupAnnotation : Annotation
{
    private protected MarkupAnnotation(PdfRect bounds) : base(bounds)
        => Areas = new TrackedList<PdfRect>(Touch) { bounds };

    /// <summary>Gets the rectangular text areas covered by the markup.</summary>
    public IList<PdfRect> Areas { get; }
}

/// <summary>Represents highlighted text.</summary>
/// <param name="bounds">The annotation bounds.</param>
public sealed class HighlightAnnotation(PdfRect bounds) : MarkupAnnotation(bounds)
{

    internal override string Subtype => "Highlight";
}

/// <summary>Represents underlined text.</summary>
/// <param name="bounds">The annotation bounds.</param>
public sealed class UnderlineAnnotation(PdfRect bounds) : MarkupAnnotation(bounds)
{

    internal override string Subtype => "Underline";
}

/// <summary>Represents struck-out text.</summary>
/// <param name="bounds">The annotation bounds.</param>
public sealed class StrikeOutAnnotation(PdfRect bounds) : MarkupAnnotation(bounds)
{

    internal override string Subtype => "StrikeOut";
}

/// <summary>Represents text marked with a squiggly underline.</summary>
/// <param name="bounds">The annotation bounds.</param>
public sealed class SquigglyAnnotation(PdfRect bounds) : MarkupAnnotation(bounds)
{

    internal override string Subtype => "Squiggly";
}
