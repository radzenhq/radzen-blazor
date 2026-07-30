using System;
using System.IO;

namespace Radzen.Documents;


/// <summary>
/// A block that renders a raster image. The source bytes are buffered when the image is created,
/// so the caller may dispose the source stream immediately afterwards.
/// </summary>
public sealed class Image : Block
{
    private double opacity = 1;

    internal override TResult Accept<TContext, TResult>(BlockVisitor<TContext, TResult> visitor, TContext context) => visitor.Visit(this, context);

    internal Image(byte[] data) => Data = data;

    internal static Image FromStream(Stream stream) => new(StreamBytes.ReadFully(stream, ResourceLimits.Default.MaxFileBytes));

    internal byte[] Data { get; }

    internal ImageInfo Info => ImageProbe.Inspect(Data);

    /// <summary>Gets or sets the rendered width. When <see langword="null"/> the natural width is used.</summary>
    public Unit? Width { get; set; }

    /// <summary>Gets or sets the rendered height. When <see langword="null"/> the natural height is used.</summary>
    public Unit? Height { get; set; }

    /// <summary>Gets or sets the horizontal alignment of the image within its container width. Defaults to <see cref="HorizontalAlignment.Left"/>.</summary>
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

    /// <summary>
    /// Gets or sets the alternate (accessibility) description of the image, carried on the
    /// image in accessible output for assistive technology to announce in place of the
    /// picture. When <see langword="null"/> (the default) no description is written.
    /// </summary>
    public string? AlternateText { get; set; }

    /// <summary>
    /// Gets or sets the replacement text of the image: the exact text the picture stands in
    /// for, carried on the image in accessible output so that extraction and reading substitute
    /// it for the picture. When <see langword="null"/> (the default) no replacement text is written.
    /// </summary>
    public string? ActualText { get; set; }

    /// <summary>
    /// Gets or sets the opacity the image is painted with, from 0 (fully transparent)
    /// to 1 (fully opaque). Defaults to 1.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="value"/> is not between 0 and 1.</exception>
    public double Opacity
    {
        get => opacity;
        set => opacity = UnitInterval.ValidatedOpacity(value, "Image");
    }

    /// <summary>
    /// Gets or sets whether the viewer should smooth the image when it is drawn at a size
    /// other than its natural pixel dimensions. When <see langword="true"/> the request for
    /// smoothing is carried with the image. Defaults to <see langword="false"/>.
    /// </summary>
    public bool Interpolate { get; set; }

    internal (Unit Width, Unit Height)? FitBox { get; private set; }

    /// <summary>
    /// Scales the image to fit within a <paramref name="maxWidth"/> x <paramref name="maxHeight"/> box
    /// while preserving aspect ratio, picking the smaller of the two scale factors so the image fits
    /// both bounds. The base aspect is taken from any explicit <see cref="Width"/>/<see cref="Height"/>,
    /// otherwise from the image's natural size.
    /// </summary>
    /// <param name="maxWidth">The maximum width of the fit box.</param>
    /// <param name="maxHeight">The maximum height of the fit box.</param>
    /// <returns>The same <see cref="Image"/> instance.</returns>
    public Image FitInBox(Unit maxWidth, Unit maxHeight)
    {
        FitBox = (maxWidth, maxHeight);
        return this;
    }
}
