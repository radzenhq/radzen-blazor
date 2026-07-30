using System.Collections.Generic;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Pdf.Emission;

namespace Radzen.Documents.Pdf.Render;

internal sealed class ImageStore(ImageDecoders decoders)
{
    private readonly ResourceNameAllocator<SourceId, EmissionImage> images = new("Im");
    private readonly ResourceNameAllocator<SourceId, EmissionImage> interpolated = new("Imo");
    private readonly Dictionary<SourceId, EmissionImage> watermarks = [];

    public EmissionImage DecodeApplied(SourceId key, in ImagePaint paint)
    {
        var decoded = DecodeBytes(key, paint.Data);
        return paint.Interpolate
            ? interpolated.GetOrAddValue(key, name => Interpolate(name, decoded))
            : decoded;
    }

    public EmissionImage DecodeWatermark(SourceId key, in ImagePaint paint)
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

    public EmissionImage DecodeBytes(SourceId key, SceneImageData data)
        => images.GetOrAddValue(key, name => Capture(name, decoders.Decode(data.Memory, ReaderLimits.Default)));

    public static DecodedImage SourceOf(EmissionImage image) => (DecodedImage)image.Identity;

    private static EmissionImage Interpolate(string name, EmissionImage decoded)
        => Capture(name, ImageDecoder.ApplyOptions(SourceOf(decoded), interpolate: true));

    private static EmissionImage Capture(string key, DecodedImage image)
        => new(
            image,
            key,
            EmissionImagePayload.Capture(image),
            image.Alpha is { } alpha ? EmissionImagePayload.Capture(alpha) : null);
}
