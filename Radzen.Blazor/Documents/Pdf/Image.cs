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
