namespace Radzen.Documents.Pdf;


/// <summary>
/// A decorated block that wraps child block-level content in a box with padding, borders,
/// a background, an optional fixed width and horizontal alignment. A container is laid out
/// as one unit and does not break across pages.
/// </summary>
public sealed class Container : Block
{
    /// <summary>Gets the block-level content of the container.</summary>
    public BlockCollection Blocks { get; } = [];

    /// <summary>Gets or sets the padding applied on every edge between the box and its content.</summary>
    public Unit Padding { get; set; }

    /// <summary>Gets the container borders, drawn on the box edges.</summary>
    public Borders Borders { get; } = new();

    /// <summary>Gets or sets the box background color, or <see langword="null"/> for none.</summary>
    public Color? Background { get; set; }

    /// <summary>
    /// Gets or sets the horizontal alignment of the box within the available width.
    /// Only observable when <see cref="Width"/> is narrower than the available width.
    /// Defaults to <see cref="HorizontalAlignment.Left"/>.
    /// </summary>
    public HorizontalAlignment Alignment { get; set; }

    /// <summary>
    /// Gets or sets the fixed box width (padding and borders included), or
    /// <see langword="null"/> to fill the available width.
    /// </summary>
    public Unit? Width { get; set; }
}
