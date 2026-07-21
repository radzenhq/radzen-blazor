using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class ImageStore
{
    private readonly Dictionary<object, GeneratedImage> images = [];
    private readonly AppliedImageCache<GeneratedImage> applied = new();
    private int appliedCount;

    public GeneratedImage Decode(Image image) => DecodeBytes(image, image.Data);

    public GeneratedImage DecodeApplied(Image source)
    {
        var generated = Decode(source);
        if (!source.HasXObjectOptions)
        {
            return generated;
        }

        return applied.Get(source, () =>
        {
            var result = ImageDecoder.ApplyOptions(generated.Image, source);
            return ReferenceEquals(result, generated.Image)
                ? generated
                : new GeneratedImage
                {
                    Key = "Imo" + appliedCount++.ToString(CultureInfo.InvariantCulture),
                    Image = result,
                };
        });
    }

    public GeneratedImage DecodeBytes(object key, byte[] data)
    {
        if (!images.TryGetValue(key, out var generated))
        {
            var xobject = ImageDecoder.Decode(data);
            generated = new GeneratedImage
            {
                Key = "Im" + images.Count.ToString(CultureInfo.InvariantCulture),
                Image = xobject,
            };
            images[key] = generated;
        }

        return generated;
    }
}
