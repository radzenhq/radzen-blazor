using Radzen.Documents.Geometry;

namespace Radzen.Documents;


/// <summary>
/// Determines how a <see cref="Container"/> arranges its child blocks.
/// </summary>
public enum ContainerLayout
{
    /// <summary>Children are stacked vertically, each below the previous one. The default.</summary>
    Stack,

    /// <summary>
    /// Children share the container box: each child is laid out from the box top-left
    /// (inset by the padding) and children are stacked in declaration order, so later
    /// children are recorded above earlier ones. The box height is the tallest child's height.
    /// The declaration order is recorded on every laid-out draw, and a renderer is free to
    /// honour it. The PDF renderer does not: it flattens a page into fixed global phases
    /// (backgrounds, then borders, then images, then text, then watermarks), so in a PDF a
    /// text child always paints above an image child regardless of declaration order.
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
    private double opacity = 1;
    private double rotation;

    internal override TResult Accept<TContext, TResult>(BlockVisitor<TContext, TResult> visitor, TContext context) => visitor.Visit(this, context);

    /// <summary>Gets the block-level content of the container.</summary>
    public BlockCollection Blocks { get; } = [];

    /// <summary>Gets or sets the padding applied on every edge between the box and its content.</summary>
    public Unit Padding { get; set; }

    /// <summary>
    /// Gets or sets the padding on the left edge, or <see langword="null"/> when the container sets none
    /// and the edge falls back to <see cref="Padding"/>. Setting <see langword="null"/> resets it.
    /// </summary>
    public Unit? PaddingLeft { get; set; }

    /// <summary>
    /// Gets or sets the padding on the right edge, or <see langword="null"/> when the container sets none
    /// and the edge falls back to <see cref="Padding"/>. Setting <see langword="null"/> resets it.
    /// </summary>
    public Unit? PaddingRight { get; set; }

    /// <summary>
    /// Gets or sets the padding on the top edge, or <see langword="null"/> when the container sets none
    /// and the edge falls back to <see cref="Padding"/>. Setting <see langword="null"/> resets it.
    /// </summary>
    public Unit? PaddingTop { get; set; }

    /// <summary>
    /// Gets or sets the padding on the bottom edge, or <see langword="null"/> when the container sets none
    /// and the edge falls back to <see cref="Padding"/>. Setting <see langword="null"/> resets it.
    /// </summary>
    public Unit? PaddingBottom { get; set; }

    internal BoxPadding EffectivePadding => new(
        (PaddingLeft ?? Padding).Point,
        (PaddingRight ?? Padding).Point,
        (PaddingTop ?? Padding).Point,
        (PaddingBottom ?? Padding).Point);

    /// <summary>Gets the container borders, drawn on the box edges.</summary>
    public Borders Borders { get; } = new();

    /// <summary>Gets or sets the box background color, or <see langword="null"/> for none.</summary>
    public Color? Background { get; set; }

    /// <summary>
    /// Gets or sets a gradient box background. Its coordinates are box-relative and top-origin:
    /// points within this container's painted box, (0, 0) at the box's top-left corner.
    /// When set it takes precedence over <see cref="Background"/>. Defaults to
    /// <see langword="null"/> (no gradient).
    /// </summary>
    public GradientBrush? BackgroundGradient { get; set; }

    /// <summary>
    /// Gets or sets the corner radius of the box. When greater than zero the background is
    /// filled as a rounded rectangle (the effective radius is clamped to half of the smaller
    /// box dimension). The border is stroked as the same rounded path only when it is uniform -
    /// the same width, color and style on all four edges; a non-uniform border keeps the square
    /// four-edge rendering while the background stays rounded. Defaults to 0 (square).
    /// </summary>
    public Unit CornerRadius { get; set; }

    /// <summary>
    /// Gets or sets the opacity the box background and borders are painted with, from 0
    /// (fully transparent) to 1 (fully opaque). Defaults to 1.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is not between 0 and 1.</exception>
    public double Opacity
    {
        get => opacity;
        set => opacity = UnitInterval.ValidatedOpacity(value, "Container");
    }

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
    /// Gets or sets the blend mode the box decoration (background and borders) is painted
    /// with. When <see langword="null"/> (the default) no blend mode is applied and the
    /// output is unchanged.
    /// </summary>
    public BlendMode? BlendMode { get; set; }

    /// <summary>
    /// Gets or sets a blurred drop shadow painted underneath the box, or <see langword="null"/>
    /// for none (the default). The shadow is decorative: it never affects layout. It is not
    /// painted for a rotated container (<see cref="Rotation"/> is non-zero).
    /// </summary>
    public BoxShadow? Shadow { get; set; }

    /// <summary>
    /// Gets or sets the rotation of the whole container content in degrees, counterclockwise,
    /// about the center of the container box. Defaults to 0 (no rotation). The rotated content
    /// is not clipped to the original box, so corners of a rotated box extend outside it.
    /// Overlay and rotated containers are only supported as direct section content, not
    /// inside table cells or other containers.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="value"/> is not finite.</exception>
    public double Rotation
    {
        get => rotation;
        set => rotation = AuthoredNumber.Finite(value, "Container.Rotation");
    }
}
