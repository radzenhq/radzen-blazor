namespace Radzen.Documents;

/// <summary>
/// A blurred drop shadow painted underneath a <see cref="Container"/> box: the shape of the box,
/// grown by <see cref="Spread"/>, filled with <see cref="Color"/>, offset by
/// <see cref="OffsetX"/> and <see cref="OffsetY"/> and softened by <see cref="BlurRadius"/>.
/// It follows the box's rounded corners and never affects layout. Leaving
/// <see cref="Container.Shadow"/> unset paints no shadow.
/// </summary>
public sealed class BoxShadow
{
    /// <summary>
    /// Gets or sets the shadow color. Its alpha channel scales the shadow opacity.
    /// Defaults to 62%-opaque black.
    /// </summary>
    public Color Color { get; set; } = Color.FromArgb(160, 0, 0, 0);

    /// <summary>
    /// Gets or sets the blur radius. Zero produces a hard-edged shadow; larger values
    /// soften the edge. Defaults to 0.
    /// </summary>
    public Unit BlurRadius { get; set; }

    /// <summary>Gets or sets the horizontal offset; positive moves the shadow right. Defaults to 0.</summary>
    public Unit OffsetX { get; set; }

    /// <summary>Gets or sets the vertical offset; positive moves the shadow down. Defaults to 0.</summary>
    public Unit OffsetY { get; set; }

    /// <summary>
    /// Gets or sets how far the shadow shape grows beyond the box on every edge before it is
    /// blurred. Negative values shrink it. Defaults to 0.
    /// </summary>
    public Unit Spread { get; set; }
}
