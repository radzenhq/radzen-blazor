using System.IO;

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
    private (double Width, double Height)? natural;

    internal InlineImage(byte[] data)
        : base(string.Empty)
        => Data = data;

    internal static InlineImage FromStream(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return new InlineImage(buffer.ToArray());
    }

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
        var (naturalWidth, naturalHeight) = natural ??= NaturalSize();
        if (width is { } w && height is { } h)
        {
            return (w.Point, h.Point);
        }

        if (width is { } wo)
        {
            return (wo.Point, naturalHeight * wo.Point / naturalWidth);
        }

        if (height is { } ho)
        {
            return (naturalWidth * ho.Point / naturalHeight, ho.Point);
        }

        return (naturalWidth, naturalHeight);
    }

    private (double Width, double Height) NaturalSize()
    {
        var dict = ImageDecoder.Decode(Data).Image.Dictionary;
        var pixelWidth = ((Objects.NumberObject)dict["Width"]).DoubleValue;
        var pixelHeight = ((Objects.NumberObject)dict["Height"]).DoubleValue;
        return (pixelWidth * 72.0 / 96.0, pixelHeight * 72.0 / 96.0);
    }
}
