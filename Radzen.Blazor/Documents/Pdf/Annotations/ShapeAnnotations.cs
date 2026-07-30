using Radzen.Documents.Fonts;

namespace Radzen.Documents.Pdf;

/// <summary>Represents a rubber-stamp annotation.</summary>
/// <param name="bounds">The annotation bounds.</param>
public sealed class StampAnnotation(PdfRect bounds) : Annotation(bounds)
{
    private string name = "Draft";

    /// <summary>Gets or sets the stamp name.</summary>
    public string Name
    {
        get => name;
        set => Set(ref name, value);
    }

    internal override string Subtype => "Stamp";
}

/// <summary>Base class for square and circle annotations.</summary>
/// <param name="bounds">The annotation bounds.</param>
public abstract class ShapeAnnotation(PdfRect bounds) : Annotation(bounds)
{
    private double borderWidth = 1;
    private Color? interiorColor;

    /// <summary>Gets or sets the border width in points.</summary>
    public double BorderWidth
    {
        get => borderWidth;
        set => Set(ref borderWidth, value);
    }

    /// <summary>Gets or sets the optional interior fill color.</summary>
    public Color? InteriorColor
    {
        get => interiorColor;
        set => Set(ref interiorColor, value);
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
    private Font font = new();
    private Color textColor = Color.Black;

    /// <summary>Gets or sets the text font.</summary>
    public Font Font
    {
        get => font;
        set => Set(ref font, value);
    }

    /// <summary>Gets or sets the font size.</summary>
    public Unit FontSize
    {
        get => Font.EffectiveSize;
        set => Font.Size = value;
    }

    /// <summary>Gets or sets the text color.</summary>
    public Color TextColor
    {
        get => textColor;
        set => Set(ref textColor, value);
    }

    /// <inheritdoc />
    public override bool IsModified => base.IsModified || Font.IsModified;

    internal override string Subtype => "FreeText";

    internal override void AcceptChanges()
    {
        base.AcceptChanges();
        Font.AcceptChanges();
    }

    internal override void OwnedBy(System.Action? changed)
    {
        base.OwnedBy(changed);
        Font.OwnedBy(changed);
    }
}
