namespace Radzen.Documents.Pdf;


/// <summary>
/// A text font: family name, size in points and style attributes.
/// </summary>
public class Font
{
    private string? name;
    private double? size;
    private bool? bold;
    private bool? italic;
    private bool? underline;
    private bool? strikethrough;
    private Color? color;

    /// <summary>Gets or sets the font family name.</summary>
    public string Name
    {
        get => name ?? "Helvetica";
        set => name = value;
    }

    /// <summary>Gets or sets the font size in points. Defaults to 10.</summary>
    public double Size
    {
        get => size ?? 10;
        set => size = value;
    }

    /// <summary>Gets or sets a value indicating whether the font is bold.</summary>
    public bool Bold
    {
        get => bold ?? false;
        set => bold = value;
    }

    /// <summary>Gets or sets a value indicating whether the font is italic.</summary>
    public bool Italic
    {
        get => italic ?? false;
        set => italic = value;
    }

    /// <summary>Gets or sets a value indicating whether the text is underlined.</summary>
    public bool Underline
    {
        get => underline ?? false;
        set => underline = value;
    }

    /// <summary>Gets or sets a value indicating whether the text is struck through.</summary>
    public bool Strikethrough
    {
        get => strikethrough ?? false;
        set => strikethrough = value;
    }

    /// <summary>Gets or sets the text color. Defaults to black.</summary>
    public Color Color
    {
        get => color ?? Color.Black;
        set => color = value;
    }

    internal string? NameValue => name;

    internal double? SizeValue => size;

    internal bool? BoldValue => bold;

    internal bool? ItalicValue => italic;

    internal bool? UnderlineValue => underline;

    internal bool? StrikethroughValue => strikethrough;

    internal Color? ColorValue => color;

    // Fills any property this font leaves unset from the next font in the cascade.
    internal void InheritFrom(Font parent)
    {
        name ??= parent.name;
        size ??= parent.size;
        bold ??= parent.bold;
        italic ??= parent.italic;
        underline ??= parent.underline;
        strikethrough ??= parent.strikethrough;
        color ??= parent.color;
    }
}
