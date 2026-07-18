using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;

namespace Radzen.Documents.Pdf.Emit;

internal static class ImageXObjectShell
{
    public static void Apply(
        DictionaryObject dict,
        DocumentObject width,
        DocumentObject height,
        DocumentObject? colorSpace,
        bool imageMask,
        DocumentObject bitsPerComponent,
        DocumentObject filter)
    {
        dict["Type"] = new NameObject("XObject");
        dict["Subtype"] = new NameObject("Image");
        dict["Width"] = width;
        dict["Height"] = height;
        if (colorSpace is not null)
        {
            dict["ColorSpace"] = colorSpace;
        }

        if (imageMask)
        {
            dict["ImageMask"] = new BooleanObject(true);
        }

        dict["BitsPerComponent"] = bitsPerComponent;
        dict["Filter"] = filter;
    }

    public static StreamObject FlateImage(byte[] samples, int width, int height, int bitsPerComponent, DocumentObject colorSpace)
    {
        var stream = new StreamObject(FlateFilter.Encode(samples));
        Apply(
            stream.Dictionary,
            new NumberObject(width),
            new NumberObject(height),
            colorSpace,
            imageMask: false,
            new NumberObject(bitsPerComponent),
            new NameObject("FlateDecode"));
        return stream;
    }
}
