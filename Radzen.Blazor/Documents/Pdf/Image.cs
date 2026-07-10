using System.IO;

namespace Radzen.Documents.Pdf;

#nullable enable

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
}
