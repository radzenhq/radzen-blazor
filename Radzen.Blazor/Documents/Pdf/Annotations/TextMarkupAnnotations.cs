using System.Collections.Generic;
using System.Text;

namespace Radzen.Documents.Pdf;

/// <summary>Represents a sticky-note text annotation.</summary>
/// <param name="bounds">The annotation bounds.</param>
public sealed class TextAnnotation(PdfRect bounds) : Annotation(bounds)
{
    /// <summary>Gets or sets whether the note is initially open.</summary>
    public bool Open { get; set; }

    /// <summary>Gets or sets the standard note icon name.</summary>
    public string Icon { get; set; } = "Note";

    internal override string Subtype => "Text";

    internal override void AppendState(StringBuilder value) => value.Append('|').Append(Open).Append('|').Append(Icon);
}

/// <summary>Base class for text markup annotations.</summary>
/// <param name="bounds">The annotation bounds.</param>
public abstract class MarkupAnnotation(PdfRect bounds) : Annotation(bounds)
{
    /// <summary>Gets the rectangular text areas covered by the markup.</summary>
    public IList<PdfRect> Areas { get; } = [bounds];

    internal override void AppendState(StringBuilder value)
    {
        foreach (var area in Areas)
        {
            Append(value, area.Left);
            Append(value, area.Bottom);
            Append(value, area.Width);
            Append(value, area.Height);
        }
    }
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
