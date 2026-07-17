using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class ImageStore
{
    private readonly Dictionary<object, GeneratedImage> images = [];

    public GeneratedImage Decode(Image image) => DecodeBytes(image, image.Data);

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
