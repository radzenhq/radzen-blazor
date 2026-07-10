namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// A text font: family name, size in points and style attributes.
/// </summary>
public class Font
{
    /// <summary>Gets or sets the font family name.</summary>
    public string Name { get; set; } = "Helvetica";

    /// <summary>Gets or sets the font size in points. Defaults to 10.</summary>
    public double Size { get; set; } = 10;

    /// <summary>Gets or sets a value indicating whether the font is bold.</summary>
    public bool Bold { get; set; }

    /// <summary>Gets or sets a value indicating whether the font is italic.</summary>
    public bool Italic { get; set; }

    /// <summary>Gets or sets a value indicating whether the text is underlined.</summary>
    public bool Underline { get; set; }

    /// <summary>Gets or sets the text color. Defaults to black.</summary>
    public Color Color { get; set; } = Colors.Black;
}
