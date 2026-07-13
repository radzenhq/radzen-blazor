using Radzen.Documents.Pdf.Objects;
using System;
using System.IO;

namespace Radzen.Documents.Pdf.Emit;

internal static class ImageDecoder
{
    // The built-in format decoders, tried in order; a new format is a new IImageDecoder
    // registered here rather than an added arm in a central magic-byte switch.
    private static readonly IImageDecoder[] Decoders =
    [
        new PngImageDecoder(),
        new JpegImageDecoder(),
        new Jpeg2000ImageDecoder(),
    ];

    public static ImageXObject Decode(byte[] imageBytes) => Decode(imageBytes, ReaderLimits.Default);

    public static ImageXObject Decode(byte[] imageBytes, ReaderLimits limits)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);
        ArgumentNullException.ThrowIfNull(limits);

        foreach (var decoder in Decoders)
        {
            if (decoder.TryDecode(imageBytes, limits, out var xobject))
            {
                return xobject;
            }
        }

        throw new NotSupportedException("Unrecognized image format; only PNG, JPEG and JPEG2000 are supported.");
    }

    // Rendered size honoring explicit Width/Height, keeping aspect when one is omitted.
    // A fully unsized image renders at 96dpi and is clamped to the available width.
    public static (double Width, double Height) Measure(Image image, ImageXObject xobject, double availableWidth)
    {
        var dict = xobject.Image.Dictionary;
        var pixelWidth = ((NumberObject)dict["Width"]).DoubleValue;
        var pixelHeight = ((NumberObject)dict["Height"]).DoubleValue;

        double baseWidth;
        double baseHeight;
        if (image.Width is { } w && image.Height is { } h)
        {
            baseWidth = w.Point;
            baseHeight = h.Point;
        }
        else if (image.Width is { } wo)
        {
            baseWidth = wo.Point;
            baseHeight = pixelHeight * wo.Point / pixelWidth;
        }
        else if (image.Height is { } ho)
        {
            baseWidth = pixelWidth * ho.Point / pixelHeight;
            baseHeight = ho.Point;
        }
        else
        {
            baseWidth = pixelWidth * 72.0 / 96.0;
            baseHeight = pixelHeight * 72.0 / 96.0;
        }

        if (image.FitBox is { } box)
        {
            var scale = Math.Min(box.Width.Point / baseWidth, box.Height.Point / baseHeight);
            return (baseWidth * scale, baseHeight * scale);
        }

        // Only a fully unsized image is clamped to the available width; an explicit dimension wins.
        if (image.Width is null && image.Height is null
            && availableWidth > 0 && !double.IsInfinity(availableWidth) && baseWidth > availableWidth)
        {
            baseHeight *= availableWidth / baseWidth;
            baseWidth = availableWidth;
        }

        return (baseWidth, baseHeight);
    }

    // Stamps the opt-in options a block Image carries onto its decoded XObject. A stencil
    // mask needs a dictionary without /ColorSpace, so it yields a fresh image-mask XObject;
    // the remaining flags are additive. An image that opts into nothing is returned untouched,
    // keeping the default output byte-identical.
    public static ImageXObject ApplyOptions(ImageXObject xobject, Image image)
    {
        if (image.Stencil && image.ColorKeyMask is not null)
        {
            throw new InvalidOperationException("Colour-key masking cannot be combined with a stencil mask.");
        }

        if (image.Stencil)
        {
            // Yields a brand-new image-mask XObject; nothing of the shared decode is touched.
            xobject = BuildStencilMask(xobject);
        }
        else if (image.ColorKeyMask is not null || image.Interpolate)
        {
            // Copy the shared decode before stamping additive dictionary options so the
            // cached plain XObject is never mutated in place (distinct-option uses of the
            // same bytes must not leak /Mask or /Interpolate onto each other).
            xobject = Copy(xobject);
        }

        if (image.ColorKeyMask is { } ranges)
        {
            ApplyColorKeyMask(xobject, ranges);
        }

        if (image.Interpolate)
        {
            xobject.Image.Dictionary["Interpolate"] = new BooleanObject(true);
        }

        return xobject;
    }

    // Shallow copy of an image XObject: a fresh stream over the same sample bytes carrying
    // a fresh dictionary with the same entries, so additive options mutate the copy alone.
    private static ImageXObject Copy(ImageXObject xobject)
    {
        var stream = new StreamObject(xobject.Image.Data);
        foreach (var key in xobject.Image.Dictionary.Keys)
        {
            stream.Dictionary[key] = xobject.Image.Dictionary[key];
        }

        return new ImageXObject(stream, xobject.SoftMask);
    }

    // Colour-key masking (ISO 32000-1 8.9.6.4): /Mask holds one inclusive [min max] pair per
    // colour component; a pixel whose components all fall within their ranges is not painted.
    private static void ApplyColorKeyMask(ImageXObject xobject, int[] ranges)
    {
        if (ranges.Length == 0 || (ranges.Length % 2) != 0)
        {
            throw new ArgumentException("A colour-key mask must be a non-empty sequence of [min max] pairs.", nameof(ranges));
        }

        if (xobject.SoftMask is not null)
        {
            throw new InvalidOperationException("Colour-key masking cannot be combined with an image that has an alpha channel.");
        }

        var dict = xobject.Image.Dictionary;
        if (ColorComponents(dict) is { } components && ranges.Length != 2 * components)
        {
            throw new ArgumentException(
                $"A colour-key mask needs one [min max] pair per colour component ({components}).", nameof(ranges));
        }

        var array = new ArrayObject();
        foreach (var value in ranges)
        {
            array.Add(new NumberObject(value));
        }

        dict["Mask"] = array;
    }

    // Colour-component count of the image's colour space, or null when it cannot be told from
    // the dictionary (a JPXDecode image carries its own colour space and has no /ColorSpace).
    private static int? ColorComponents(DictionaryObject dict)
    {
        if (!dict.TryGetValue("ColorSpace", out var colorSpace) || colorSpace is null)
        {
            return null;
        }

        return colorSpace switch
        {
            NameObject { Value: "DeviceGray" } => 1,
            NameObject { Value: "DeviceRGB" } => 3,
            NameObject { Value: "DeviceCMYK" } => 4,
            ArrayObject { Count: > 0 } array when array[0] is NameObject { Value: "Indexed" } => 1,
            _ => null,
        };
    }

    // Re-expresses a 1-bit grayscale image as a stencil mask (ISO 32000-1 8.9.6.2): the same
    // packed 1-bit sample stream, but with /ImageMask true and no /ColorSpace so its samples
    // gate painting in the current fill colour (sample 0 paints, per the default /Decode [0 1]).
    private static ImageXObject BuildStencilMask(ImageXObject xobject)
    {
        var source = xobject.Image.Dictionary;
        if (xobject.SoftMask is not null
            || source["ColorSpace"] is not NameObject { Value: "DeviceGray" }
            || ((NumberObject)source["BitsPerComponent"]).IntValue != 1)
        {
            throw new InvalidOperationException(
                "A stencil mask requires a 1-bit grayscale image with no alpha channel.");
        }

        var stream = new StreamObject(xobject.Image.Data);
        var dict = stream.Dictionary;
        dict["Type"] = new NameObject("XObject");
        dict["Subtype"] = new NameObject("Image");
        dict["Width"] = source["Width"];
        dict["Height"] = source["Height"];
        dict["ImageMask"] = new BooleanObject(true);
        dict["BitsPerComponent"] = new NumberObject(1);
        dict["Filter"] = source["Filter"];
        return new ImageXObject(stream, null);
    }

    // Header dimensions drive every downstream pixel buffer; reject non-positive or
    // oversized geometry before allocating so a tiny header cannot request gigabytes
    // or wrap width*height negative.
    internal static void ValidateImageDimensions(long width, long height, ReaderLimits limits, string format)
    {
        if (width <= 0 || height <= 0)
        {
            throw new InvalidDataException($"{format} image has invalid dimensions.");
        }

        if (width * height > limits.MaxImagePixels)
        {
            throw new InvalidDataException($"{format} image dimensions exceed the maximum decodable size.");
        }
    }
}
