namespace Radzen.Documents.Pdf;


/// <summary>
/// A text font: family name, size in points and style attributes.
/// </summary>
public class Font : ITracksChanges
{
    private string? name;
    private double? size;
    private bool? bold;
    private bool? italic;
    private bool? underline;
    private bool? strikethrough;
    private Color? color;
    private ChangeTracker tracker;

    /// <summary>Gets or sets the font family name.</summary>
    public string Name
    {
        get => name ?? "Helvetica";
        set => tracker.Set(ref name, value);
    }

    /// <summary>Gets or sets the font size in points. Defaults to 10.</summary>
    public double Size
    {
        get => size ?? 10;
        set => tracker.Set(ref size, value);
    }

    /// <summary>Gets or sets a value indicating whether the font is bold.</summary>
    public bool Bold
    {
        get => bold ?? false;
        set => tracker.Set(ref bold, value);
    }

    /// <summary>Gets or sets a value indicating whether the font is italic.</summary>
    public bool Italic
    {
        get => italic ?? false;
        set => tracker.Set(ref italic, value);
    }

    /// <summary>Gets or sets a value indicating whether the text is underlined.</summary>
    public bool Underline
    {
        get => underline ?? false;
        set => tracker.Set(ref underline, value);
    }

    /// <summary>Gets or sets a value indicating whether the text is struck through.</summary>
    public bool Strikethrough
    {
        get => strikethrough ?? false;
        set => tracker.Set(ref strikethrough, value);
    }

    /// <summary>Gets or sets the text color. Defaults to black.</summary>
    public Color Color
    {
        get => color ?? Color.Black;
        set => tracker.Set(ref color, value);
    }

    /// <summary>
    /// Gets a value indicating whether this font has been assigned to since it was
    /// materialized. A <see cref="TextContent"/> folds its own font's state into
    /// <see cref="ContentElement.IsModified"/>.
    /// </summary>
    public bool IsModified => tracker.IsModified;

    internal void AcceptChanges() => tracker.AcceptChanges();

    void ITracksChanges.AcceptChanges() => AcceptChanges();

    internal string? NameValue => name;

    internal double? SizeValue => size;

    internal bool? BoldValue => bold;

    internal bool? ItalicValue => italic;

    internal bool? UnderlineValue => underline;

    internal bool? StrikethroughValue => strikethrough;

    internal Color? ColorValue => color;

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
