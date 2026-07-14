using System.Globalization;
using System.Text;

namespace Radzen.Documents.Pdf;

/// <summary>Describes an interactive annotation placed on a PDF page.</summary>
/// <param name="bounds">The annotation bounds in PDF page coordinates.</param>
public abstract class Annotation(Rect bounds)
{
    /// <summary>Gets or sets the annotation bounds in PDF page coordinates.</summary>
    public Rect Bounds { get; set; } = bounds;

    /// <summary>Gets or sets the annotation color.</summary>
    public Color Color { get; set; } = Color.Yellow;

    /// <summary>Gets or sets the annotation opacity from 0 to 1.</summary>
    public double Opacity { get; set; } = 1;

    /// <summary>Gets or sets the annotation flags.</summary>
    public AnnotationFlags Flags { get; set; } = AnnotationFlags.Print;

    /// <summary>Gets or sets the annotation text.</summary>
    public string? Contents { get; set; }

    /// <summary>Gets or sets the annotation title or author.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets a custom appearance.</summary>
    public AnnotationAppearance? Appearance { get; set; }

    internal abstract string Subtype { get; }

    internal string State()
    {
        var value = new StringBuilder();
        Append(value, Bounds.X);
        Append(value, Bounds.Y);
        Append(value, Bounds.Width);
        Append(value, Bounds.Height);
        value.Append('|').Append(Color.A).Append(',').Append(Color.R).Append(',').Append(Color.G).Append(',').Append(Color.B);
        Append(value, Opacity);
        value.Append('|').Append((int)Flags).Append('|').Append(Contents).Append('|').Append(Title);
        value.Append('|').Append(Appearance is null ? -1 : Appearance.Content.Count);
        AppendState(value);
        return value.ToString();
    }

    internal virtual void AppendState(StringBuilder value)
    {
    }

    internal static void Append(StringBuilder value, double number)
        => value.Append('|').Append(number.ToString("R", CultureInfo.InvariantCulture));
}
