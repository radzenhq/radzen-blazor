using System.IO;

namespace Radzen.Documents;


/// <summary>
/// A raster image that flows inline within a paragraph line, sitting on the line's baseline and
/// advancing it by the image width. The source bytes are buffered when the image is created,
/// so the caller may dispose the source stream immediately afterwards.
/// </summary>
public sealed class InlineImage : Inline
{
    private (double Width, double Height)? pixels;

    internal InlineImage(byte[] data) => Data = data;

    internal static InlineImage FromStream(Stream stream) => new(StreamBytes.ReadFully(stream, ResourceLimits.Default.MaxFileBytes));

    internal byte[] Data { get; }

    internal ImageInfo Info => ImageProbe.Inspect(Data);

    /// <summary>
    /// Gets or sets the drawn width, or <see langword="null"/> (the default) for the natural width.
    /// When only <see cref="Height"/> is set the width follows from it and the image's aspect ratio.
    /// </summary>
    public Unit? Width { get; set; }

    /// <summary>
    /// Gets or sets the drawn height, or <see langword="null"/> (the default) for the natural height.
    /// When only <see cref="Width"/> is set the height follows from it and the image's aspect ratio.
    /// </summary>
    public Unit? Height { get; set; }

    /// <summary>
    /// Gets or sets the alternate (accessibility) description of the image, carried on the
    /// image in accessible output for assistive technology to announce in place of the
    /// picture. When <see langword="null"/> or empty (the default) the image is decorative and
    /// carries no description.
    /// </summary>
    public string? AlternateText { get; set; }

    internal (double Width, double Height) EffectiveSize()
    {
        var (pixelWidth, pixelHeight) = pixels ??= ImageProbe.PixelSize(Data);
        return ImageProbe.DeriveSize(Width, Height, pixelWidth, pixelHeight);
    }
}
