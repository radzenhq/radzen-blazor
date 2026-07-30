using System.Collections.Generic;
using System.Globalization;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class ImageStore
{
    private readonly Dictionary<object, GeneratedImage> legacyImages = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<(object Key, bool Interpolate), GeneratedImage> legacyApplied = [];
    private readonly Dictionary<(object Key, bool Interpolate), GeneratedImage> legacyWatermarks = [];
    private readonly Dictionary<SourceId, GeneratedImage> images = [];
    private readonly Dictionary<(SourceId Key, bool Interpolate), GeneratedImage> applied = [];
    private readonly Dictionary<(SourceId Key, bool Interpolate), GeneratedImage> watermarks = [];
    private int imageCount;
    private int appliedCount;

    public GeneratedImage Decode(Image image) => DecodeBytes(image, image.Data);

    public GeneratedImage DecodeApplied(Image source)
    {
        var generated = DecodeBytes(source, source.Data);
        if (!source.Interpolate)
        {
            return generated;
        }

        var cacheKey = ((object)source, source.Interpolate);
        if (!legacyApplied.TryGetValue(cacheKey, out var result))
        {
            var image = ImageDecoder.ApplyOptions(generated.Image, source.Interpolate);
            result = ReferenceEquals(image, generated.Image)
                ? generated
                : new GeneratedImage
                {
                    Key = "Imo" + appliedCount++.ToString(CultureInfo.InvariantCulture),
                    Image = image,
                };
            legacyApplied[cacheKey] = result;
        }

        return result;
    }

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

    public GeneratedImage DecodeWatermark(Image image)
    {
        var generated = DecodeBytes(image, image.Data);
        if (!image.Interpolate)
        {
            return generated;
        }

        var cacheKey = ((object)image, image.Interpolate);
        if (!legacyWatermarks.TryGetValue(cacheKey, out var result))
        {
            result = new GeneratedImage
            {
                Key = generated.Key + "w",
                Image = ImageDecoder.ApplyOptions(generated.Image, image.Interpolate),
            };
            legacyWatermarks[cacheKey] = result;
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

    public GeneratedImage DecodeBytes(object key, byte[] data)
    {
        if (!legacyImages.TryGetValue(key, out var generated))
        {
            generated = new GeneratedImage
            {
                Key = "Im" + imageCount++.ToString(CultureInfo.InvariantCulture),
                Image = ImageDecoder.Decode(data),
            };
            legacyImages[key] = generated;
        }

        return generated;
    }
}
