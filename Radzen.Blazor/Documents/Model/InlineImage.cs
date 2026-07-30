using System.IO;

namespace Radzen.Documents;


/// <summary>
/// A raster image that flows inline within a paragraph line, sharing the line's baseline and
/// advancing it by the image width. The source bytes are buffered when the image is created,
/// so the caller may dispose the source stream immediately afterwards.
/// </summary>
public sealed class InlineImage : Run
{
    private Unit? width;
    private Unit? height;
    private (double Width, double Height)? pixels;

    internal InlineImage(byte[] data)
        : base(string.Empty)
        => Data = data;

    internal static InlineImage FromStream(Stream stream) => new(StreamBytes.ReadFully(stream, ResourceLimits.Default.MaxFileBytes));

    internal byte[] Data { get; }

    internal ImageInfo Info => ImageProbe.Inspect(Data);

    /// <summary>Gets or sets the inline width. When unset the natural width is used, deriving from <see cref="Height"/> when only that is set.</summary>
    public Unit Width
    {
        get => Unit.FromPoint(EffectiveSize().Width);
        set => width = value;
    }

    /// <summary>Gets or sets the inline height. When unset the natural height is used, deriving from <see cref="Width"/> when only that is set.</summary>
    public Unit Height
    {
        get => Unit.FromPoint(EffectiveSize().Height);
        set => height = value;
    }

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
        return ImageProbe.DeriveSize(width, height, pixelWidth, pixelHeight);
    }
}
