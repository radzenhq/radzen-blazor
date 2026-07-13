using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Emit;

/// <summary>
/// A decoded image resource: the image XObject stream and its optional soft-mask stream.
/// Produced by an <see cref="IImageDecoder"/> and painted through the content pipeline.
/// </summary>
/// <param name="image">The image XObject stream carrying the sample data and its dictionary.</param>
/// <param name="softMask">The soft-mask (alpha) stream, or <c>null</c> when the image is opaque.</param>
public sealed class ImageXObject(StreamObject image, StreamObject? softMask)
{
    /// <summary>Gets the image XObject stream carrying the sample data and its dictionary.</summary>
    public StreamObject Image { get; } = image;

    /// <summary>Gets the soft-mask (alpha) stream, or <c>null</c> when the image is opaque.</summary>
    public StreamObject? SoftMask { get; } = softMask;
}
