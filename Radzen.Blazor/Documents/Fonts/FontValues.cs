namespace Radzen.Documents.Fonts;

/// <summary>
/// The effective font values of an element, with every value resolved through the inheritance chain
/// down to the built-in default.
/// </summary>
public readonly record struct FontValues
{
    /// <summary>Gets the effective font family name. The default, <c>"Helvetica"</c>, names the built-in
    /// metric-compatible sans family shipped with the library rather than the Adobe typeface.</summary>
    public required string Family { get; init; }

    /// <summary>Gets the effective font size.</summary>
    public required Unit Size { get; init; }

    /// <summary>Gets a value indicating whether the effective font is bold.</summary>
    public required bool Bold { get; init; }

    /// <summary>Gets a value indicating whether the effective font is italic.</summary>
    public required bool Italic { get; init; }

    /// <summary>Gets a value indicating whether the effective font is underlined.</summary>
    public required bool Underline { get; init; }

    /// <summary>Gets a value indicating whether the effective font is struck through.</summary>
    public required bool Strikethrough { get; init; }

    /// <summary>Gets the effective text color.</summary>
    public required Color Color { get; init; }
}
