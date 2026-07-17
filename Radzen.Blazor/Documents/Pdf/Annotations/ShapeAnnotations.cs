using System.Text;

namespace Radzen.Documents.Pdf;

/// <summary>Represents a rubber-stamp annotation.</summary>
/// <param name="bounds">The annotation bounds.</param>
public sealed class StampAnnotation(PdfRect bounds) : Annotation(bounds)
{
    /// <summary>Gets or sets the stamp name.</summary>
    public string Name { get; set; } = "Draft";

    internal override string Subtype => "Stamp";

    internal override void AppendState(StringBuilder value) => value.Append('|').Append(Name);
}

/// <summary>Base class for square and circle annotations.</summary>
/// <param name="bounds">The annotation bounds.</param>
public abstract class ShapeAnnotation(PdfRect bounds) : Annotation(bounds)
{
    /// <summary>Gets or sets the border width in points.</summary>
    public double BorderWidth { get; set; } = 1;

    /// <summary>Gets or sets the optional interior fill color.</summary>
    public Color? InteriorColor { get; set; }

    internal override void AppendState(StringBuilder value)
    {
        Append(value, BorderWidth);
        value.Append('|').Append(InteriorColor?.A).Append(',').Append(InteriorColor?.R).Append(',')
            .Append(InteriorColor?.G).Append(',').Append(InteriorColor?.B);
    }
}

/// <summary>Represents a square or rectangular annotation.</summary>
/// <param name="bounds">The annotation bounds.</param>
public sealed class SquareAnnotation(PdfRect bounds) : ShapeAnnotation(bounds)
{

    internal override string Subtype => "Square";
}

/// <summary>Represents a circular or elliptical annotation.</summary>
/// <param name="bounds">The annotation bounds.</param>
public sealed class CircleAnnotation(PdfRect bounds) : ShapeAnnotation(bounds)
{

    internal override string Subtype => "Circle";
}

/// <summary>Represents text displayed directly on the page.</summary>
/// <param name="bounds">The annotation bounds.</param>
public sealed class FreeTextAnnotation(PdfRect bounds) : Annotation(bounds)
{

    /// <summary>Gets or sets the text font.</summary>
    public Font Font { get; set; } = new();

    /// <summary>Gets or sets the font size in points.</summary>
    public double FontSize
    {
        get => Font.Size;
        set => Font.Size = value;
    }

    /// <summary>Gets or sets the text color.</summary>
    public Color TextColor { get; set; } = Color.Black;

    internal override string Subtype => "FreeText";

    internal override void AppendState(StringBuilder value)
    {
        value.Append('|').Append(Font.Name);
        Append(value, Font.Size);
        value.Append('|').Append(TextColor.A).Append(',').Append(TextColor.R).Append(',').Append(TextColor.G).Append(',').Append(TextColor.B);
    }
}
