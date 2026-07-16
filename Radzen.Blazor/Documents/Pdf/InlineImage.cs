using System.IO;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Documents.Pdf;


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

    internal static InlineImage FromStream(Stream stream) => new(ImageDecoder.ReadFully(stream));

    /// <summary>The buffered image bytes.</summary>
    internal byte[] Data { get; }

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

    internal (double Width, double Height) EffectiveSize()
    {
        var (pixelWidth, pixelHeight) = pixels ??= ImageDecoder.PixelSize(ImageDecoder.Decode(Data));
        return ImageDecoder.DeriveSize(width, height, pixelWidth, pixelHeight);
    }
}
