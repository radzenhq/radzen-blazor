namespace Radzen.Documents.Pdf;


/// <summary>
/// Determines how a <see cref="Container"/> arranges its child blocks.
/// </summary>
public enum ContainerLayout
{
    /// <summary>Children are stacked vertically, each below the previous one. The default.</summary>
    Stack,

    /// <summary>
    /// Children share the container box: each child is laid out from the box top-left
    /// (inset by the padding) and children are painted in declaration order, so later
    /// children appear on top of earlier ones. The box height is the tallest child's height.
    /// Painting order across different content kinds follows the page-wide layering
    /// (backgrounds, then borders, then images, then text), so a text child always paints
    /// above an image child regardless of declaration order.
    /// </summary>
    Overlay,
}

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
    /// Gets or sets the opacity the box background and borders are painted with, from 0
    /// (fully transparent) to 1 (fully opaque). Defaults to 1.
    /// </summary>
    public double Opacity { get; set; } = 1;

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

    /// <summary>
    /// Gets or sets how the child blocks are arranged inside the box.
    /// Defaults to <see cref="ContainerLayout.Stack"/> (vertical stacking).
    /// </summary>
    public ContainerLayout Layout { get; set; }

    /// <summary>
    /// Gets or sets the rotation of the whole container content in degrees, counterclockwise,
    /// about the center of the container box. Defaults to 0 (no rotation). The rotated content
    /// is not clipped to the original box, so corners of a rotated box extend outside it.
    /// Overlay and rotated containers are only supported as direct section content, not
    /// inside table cells or other containers.
    /// </summary>
    public double Rotation { get; set; }
}
