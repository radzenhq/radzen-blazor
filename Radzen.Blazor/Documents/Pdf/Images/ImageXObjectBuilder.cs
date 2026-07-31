using System.Text;
using Radzen.Documents.Pdf.Output;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;

namespace Radzen.Documents.Pdf;

internal static class ImageXObjectBuilder
{
    // ISO 32000-1 8.9.5: an image XObject names its size, color space, sample depth and filter.
    public static StreamObject Build(OutputImagePayload image)
    {
        var stream = new StreamObject(Encode(image));
        var dict = stream.Dictionary;
        dict["Type"] = new NameObject("XObject");
        dict["Subtype"] = new NameObject("Image");
        dict["Width"] = new NumberObject(image.Width);
        dict["Height"] = new NumberObject(image.Height);
        if (ColorSpace(image) is { } colorSpace)
        {
            dict["ColorSpace"] = colorSpace;
        }

        dict["BitsPerComponent"] = new NumberObject(image.BitsPerComponent);
        dict["Filter"] = new NameObject(FilterName(image.Compression));

        if (image.ColorKeyMask.Length > 0)
        {
            var mask = new ArrayObject();
            foreach (var value in image.ColorKeyMask)
            {
                mask.Add(new NumberObject(value));
            }

            dict["Mask"] = mask;
        }

        if (image.Decode.Length > 0)
        {
            var decode = new ArrayObject();
            foreach (var value in image.Decode)
            {
                decode.Add(new NumberObject(value));
            }

            dict["Decode"] = decode;
        }

        if (image.Interpolate)
        {
            dict["Interpolate"] = new BooleanObject(true);
        }

        return stream;
    }

    private static byte[] Encode(OutputImagePayload image)
        => image.Compression == ImageCompression.None
            ? FlateFilter.Encode(image.Samples)
            : image.Samples.ToArray();

    private static string FilterName(ImageCompression compression) => compression switch
    {
        ImageCompression.Jpeg => "DCTDecode",
        ImageCompression.Jpeg2000 => "JPXDecode",
        _ => "FlateDecode",
    };

    // ISO 32000-1 8.6.6.3: an indexed color space is [/Indexed base hival lookup].
    private static DocumentObject? ColorSpace(OutputImagePayload image) => image.ColorSpace switch
    {
        ImageColorSpace.DeviceGray => new NameObject("DeviceGray"),
        ImageColorSpace.DeviceRgb => new NameObject("DeviceRGB"),
        ImageColorSpace.DeviceCmyk => new NameObject("DeviceCMYK"),
        ImageColorSpace.Indexed => new ArrayObject
        {
            new NameObject("Indexed"),
            new NameObject("DeviceRGB"),
            new NumberObject((image.Palette.Length / 3) - 1),
            new StringObject(Encoding.Latin1.GetString(image.Palette)),
        },
        _ => null,
    };
}
