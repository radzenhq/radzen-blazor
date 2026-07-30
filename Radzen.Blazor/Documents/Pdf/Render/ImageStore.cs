using System.Collections.Generic;
using System.Globalization;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Render;

internal sealed class ImageStore
{
    private readonly Dictionary<SourceId, GeneratedImage> images = [];
    private readonly Dictionary<(SourceId Key, bool Interpolate), GeneratedImage> applied = [];
    private readonly Dictionary<(SourceId Key, bool Interpolate), GeneratedImage> watermarks = [];
    private int imageCount;
    private int appliedCount;

    public GeneratedImage DecodeApplied(SourceId key, in ImagePaint paint)
    {
        var generated = DecodeBytes(key, paint.Data);
        if (!paint.Interpolate)
        {
            return generated;
        }

        var cacheKey = (key, paint.Interpolate);
        if (!applied.TryGetValue(cacheKey, out var result))
        {
            var image = ImageDecoder.ApplyOptions(generated.Image, paint.Interpolate);
            result = ReferenceEquals(image, generated.Image)
                ? generated
                : new GeneratedImage
                {
                    Key = "Imo" + appliedCount++.ToString(CultureInfo.InvariantCulture),
                    Image = image,
                };
            applied[cacheKey] = result;
        }

        return result;
    }

    public GeneratedImage DecodeWatermark(SourceId key, in ImagePaint paint)
    {
        var generated = DecodeBytes(key, paint.Data);
        if (!paint.Interpolate)
        {
            return generated;
        }

        var cacheKey = (key, paint.Interpolate);
        if (!watermarks.TryGetValue(cacheKey, out var result))
        {
            result = new GeneratedImage
            {
                Key = generated.Key + "w",
                Image = ImageDecoder.ApplyOptions(generated.Image, paint.Interpolate),
            };
            watermarks[cacheKey] = result;
        }

        return result;
    }

    public GeneratedImage DecodeBytes(SourceId key, SceneImageData data)
    {
        if (!images.TryGetValue(key, out var generated))
        {
            var xobject = ImageDecoder.Decode(data.Memory);
            generated = new GeneratedImage
            {
                Key = "Im" + imageCount++.ToString(CultureInfo.InvariantCulture),
                Image = xobject,
            };
            images[key] = generated;
        }

        return generated;
    }
}
