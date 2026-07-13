using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class ImageXObject(StreamObject image, StreamObject? softMask)
{
    public StreamObject Image { get; } = image;

    public StreamObject? SoftMask { get; } = softMask;
}
