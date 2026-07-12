using System.IO;

namespace Radzen.Documents.Pdf;


/// <summary>
/// A block that renders a raster image. The source bytes are buffered when the image is created,
/// so the caller may dispose the source stream immediately afterwards.
/// </summary>
public sealed class Image : Block
{
    internal Image(byte[] data) => Data = data;

    internal static Image FromStream(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return new Image(buffer.ToArray());
    }

    /// <summary>The buffered image bytes.</summary>
    internal byte[] Data { get; }

    /// <summary>Gets or sets the rendered width. When <see langword="null"/> the natural width is used.</summary>
    public Unit? Width { get; set; }

    /// <summary>Gets or sets the rendered height. When <see langword="null"/> the natural height is used.</summary>
    public Unit? Height { get; set; }

    /// <summary>Gets or sets the horizontal alignment of the image within its container width. Defaults to <see cref="HorizontalAlignment.Left"/>.</summary>
    public HorizontalAlignment Alignment { get; set; } = HorizontalAlignment.Left;

    /// <summary>
    /// Gets or sets the opacity the image is painted with, from 0 (fully transparent)
    /// to 1 (fully opaque). Defaults to 1.
    /// </summary>
    public double Opacity { get; set; } = 1;

    /// <summary>
    /// Gets or sets whether the viewer should smooth the image when it is drawn at a size
    /// other than its natural pixel dimensions. When <see langword="true"/> the image XObject
    /// carries <c>/Interpolate true</c> (ISO 32000-1 8.9.5.3). Defaults to <see langword="false"/>.
    /// </summary>
    public bool Interpolate { get; set; }

    /// <summary>
    /// Gets or sets whether the image is painted as a 1-bit stencil mask (<c>/ImageMask true</c>,
    /// ISO 32000-1 8.9.6.2): its samples select which pixels are painted in the current fill
    /// colour rather than carrying colour of their own. The source must decode to a 1-bit
    /// grayscale image with no alpha channel. Defaults to <see langword="false"/>.
    /// </summary>
    public bool Stencil { get; set; }

    /// <summary>
    /// Gets or sets the colour-key masking ranges (<c>/Mask</c>, ISO 32000-1 8.9.6.4). The array
    /// holds one inclusive <c>[min max]</c> pair per colour component, in the image's own sample
    /// range; a pixel whose components all fall within their ranges is left unpainted. When
    /// <see langword="null"/> (the default) no colour-key mask is applied.
    /// </summary>
    public int[]? ColorKeyMask { get; set; }

    // True when the image opts into any XObject-dictionary option, so emission can keep the
    // default path (and its bytes) untouched for an image that uses none of them.
    internal bool HasXObjectOptions => Interpolate || Stencil || ColorKeyMask is not null;

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
