using Radzen.Documents.Pdf.Objects;
using System;
using System.IO;

namespace Radzen.Documents.Pdf.Emit;

/// <summary>
/// Decodes an encoded image payload into an <see cref="ImageXObject"/>. Additional image
/// formats plug in through <see cref="Register(IImageDecoder)"/>; the built-in PNG, JPEG and
/// JPEG2000 decoders are tried first, then registered decoders in registration order.
/// </summary>
public static class ImageDecoder
{
    private const int RegisteredDecoderLimit = 64;

    private static readonly IImageDecoder[] Decoders =
    [
        new PngImageDecoder(),
        new JpegImageDecoder(),
        new Jpeg2000ImageDecoder(),
    ];

    private static volatile IImageDecoder[] registered = [];

    private static readonly object RegisterGate = new();

    /// <summary>
    /// Registers a custom <see cref="IImageDecoder"/> so a new image format can be decoded.
    /// The built-in PNG, JPEG and JPEG2000 decoders are tried first; registered decoders are
    /// tried after them in registration order.
    /// </summary>
    /// <param name="decoder">The decoder to register.</param>
    public static void Register(IImageDecoder decoder)
    {
        ArgumentNullException.ThrowIfNull(decoder);
        lock (RegisterGate)
        {
            var snapshot = registered;
            foreach (var existing in snapshot)
            {
                if (ReferenceEquals(existing, decoder))
                {
                    return;
                }
            }

            if (snapshot.Length >= RegisteredDecoderLimit)
            {
                throw new InvalidOperationException($"No more than {RegisteredDecoderLimit} custom image decoders can be registered.");
            }

            var updated = new IImageDecoder[snapshot.Length + 1];
            snapshot.CopyTo(updated, 0);
            updated[^1] = decoder;
            registered = updated;
        }
    }

    internal static byte[] ReadFully(Stream stream) => ReadFully(stream, ReaderLimits.Default);

    internal static byte[] ReadFully(Stream stream, ReaderLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        return DocumentReader.ReadFully(stream, limits.MaxFileBytes);
    }

    internal static ImageXObject Decode(byte[] imageBytes) => Decode(imageBytes, ReaderLimits.Default);

    internal static ImageXObject Decode(byte[] imageBytes, ReaderLimits limits)
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

        foreach (var decoder in registered)
        {
            if (decoder.TryDecode(imageBytes, limits, out var xobject))
            {
                return xobject;
            }
        }

        throw new NotSupportedException("Unrecognized image format; only PNG, JPEG and JPEG2000 are supported.");
    }

    internal static (double Width, double Height) PixelSize(ImageXObject xobject)
    {
        var dict = xobject.Image.Dictionary;
        return (((NumberObject)dict["Width"]).DoubleValue, ((NumberObject)dict["Height"]).DoubleValue);
    }

    internal static (double Width, double Height) DeriveSize(Unit? width, Unit? height, double pixelWidth, double pixelHeight)
    {
        if (width is { } w && height is { } h)
        {
            return (w.Point, h.Point);
        }

        if (width is { } wo)
        {
            return (wo.Point, pixelHeight * wo.Point / pixelWidth);
        }

        if (height is { } ho)
        {
            return (pixelWidth * ho.Point / pixelHeight, ho.Point);
        }

        return (pixelWidth * 72.0 / 96.0, pixelHeight * 72.0 / 96.0);
    }

    internal static (double Width, double Height) Measure(Image image, ImageXObject xobject, double availableWidth)
    {
        var (pixelWidth, pixelHeight) = PixelSize(xobject);
        var (baseWidth, baseHeight) = DeriveSize(image.Width, image.Height, pixelWidth, pixelHeight);

        if (image.FitBox is { } box)
        {
            var scale = Math.Min(box.Width.Point / baseWidth, box.Height.Point / baseHeight);
            return (baseWidth * scale, baseHeight * scale);
        }

        if (image.Width is null && image.Height is null
            && availableWidth > 0 && !double.IsInfinity(availableWidth) && baseWidth > availableWidth)
        {
            baseHeight *= availableWidth / baseWidth;
            baseWidth = availableWidth;
        }

        return (baseWidth, baseHeight);
    }

    internal static ImageXObject ApplyOptions(ImageXObject xobject, Image image)
    {
        if (image.Stencil && image.ColorKeyMask is not null)
        {
            throw new InvalidOperationException("Colour-key masking cannot be combined with a stencil mask.");
        }

        if (image.Stencil)
        {
            xobject = BuildStencilMask(xobject);
        }
        else if (image.ColorKeyMask is not null || image.Interpolate)
        {
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

    private static ImageXObject Copy(ImageXObject xobject)
    {
        var stream = new StreamObject(xobject.Image.Data);
        xobject.Image.Dictionary.Copy(stream.Dictionary);

        return new ImageXObject(stream, xobject.SoftMask);
    }

    // Colour-key masking (ISO 32000-1 8.9.6.4): /Mask holds one [min max] pair per colour component.
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

    // Stencil mask (ISO 32000-1 8.9.6.2): /ImageMask true, no /ColorSpace; sample 0 paints (default /Decode [0 1]).
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
        ImageXObjectShell.Apply(
            stream.Dictionary,
            source["Width"],
            source["Height"],
            colorSpace: null,
            imageMask: true,
            new NumberObject(1),
            source["Filter"]);
        return new ImageXObject(stream, null);
    }

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
