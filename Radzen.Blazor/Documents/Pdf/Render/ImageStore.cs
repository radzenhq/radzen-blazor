using System.Collections.Generic;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Output;

namespace Radzen.Documents.Pdf.Render;

internal sealed class ImageStore(ImageDecoders decoders)
{
    private readonly ResourceNameAllocator<SourceId, OutputImage> images = new("Im");
    private readonly ResourceNameAllocator<SourceId, OutputImage> interpolated = new("Imo");
    private readonly Dictionary<SourceId, OutputImage> watermarks = [];

    public OutputImage DecodeApplied(SourceId key, in ImagePaint paint)
    {
        var decoded = DecodeBytes(key, paint.Data);
        return paint.Interpolate
            ? interpolated.GetOrAddValue(key, name => Interpolate(name, decoded))
            : decoded;
    }

    public OutputImage DecodeWatermark(SourceId key, in ImagePaint paint)
    {
        var decoded = DecodeBytes(key, paint.Data);
        if (!paint.Interpolate)
        {
            return decoded;
        }

        if (!watermarks.TryGetValue(key, out var result))
        {
            result = Interpolate(ResourceNameAllocator.Derive(decoded.Key, "w"), decoded);
            watermarks[key] = result;
        }

        return result;
    }

    public OutputImage DecodeBytes(SourceId key, SceneImageData data)
        => images.GetOrAddValue(key, name => Capture(name, decoders.Decode(data.Memory, ReaderLimits.Default)));

    public static DecodedImage SourceOf(OutputImage image) => image.Identity;

    private static OutputImage Interpolate(string name, OutputImage decoded)
        => Capture(name, ImageDecoder.ApplyOptions(SourceOf(decoded), interpolate: true));

    private static OutputImage Capture(string key, DecodedImage image)
        => new(
            image,
            key,
            OutputImagePayload.Capture(image),
            image.Alpha is { } alpha ? OutputImagePayload.Capture(alpha) : null);
}
