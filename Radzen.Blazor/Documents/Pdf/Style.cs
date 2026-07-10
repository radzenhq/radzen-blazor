namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// A named style definition. Inheritance is resolved at layout time; this type only stores the structure.
/// </summary>
public class Style
{
    internal Style(string name, string? baseStyle)
    {
        Name = name;
        BaseStyle = baseStyle;
    }

    /// <summary>Gets the unique style name.</summary>
    public string Name { get; }

    /// <summary>Gets or sets the name of the style this one inherits from, or <see langword="null"/> for none.</summary>
    public string? BaseStyle { get; set; }

    /// <summary>Gets the style font.</summary>
    public Font Font { get; } = new();

    private HorizontalAlignment? alignment;

    /// <summary>Gets or sets the horizontal alignment. Defaults to <see cref="HorizontalAlignment.Left"/>.</summary>
    public HorizontalAlignment Alignment
    {
        get => alignment ?? HorizontalAlignment.Left;
        set => alignment = value;
    }

    internal HorizontalAlignment? AlignmentValue => alignment;
}
