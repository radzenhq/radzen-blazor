using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Documents.Pdf.Emit;

// Decodes each distinct image payload once and hands back a stable GeneratedImage,
// keyed by the source object (or the raw byte[] for inline images) so a picture reused
// across pages shares one XObject.
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
