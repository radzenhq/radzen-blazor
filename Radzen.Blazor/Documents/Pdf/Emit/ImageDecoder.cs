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
    /// tried after them in registration order. Registration also bridges the decoder to the
    /// renderer-neutral size probe layout measures with, so a custom format paginates the same
    /// way in every renderer.
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

        ImageProbe.RegisterSizeProbe(data => SizeOf(decoder, data));
    }

    private static (double Width, double Height)? SizeOf(IImageDecoder decoder, ReadOnlyMemory<byte> data)
        => decoder.TryDecode(data, ReaderLimits.Default, out var xobject) ? PixelSize(xobject) : null;

    internal static byte[] ReadFully(Stream stream) => ReadFully(stream, ReaderLimits.Default);

    internal static byte[] ReadFully(Stream stream, ReaderLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        return StreamBytes.ReadFully(stream, limits.MaxFileBytes);
    }

    internal static ImageXObject Decode(byte[] imageBytes) => Decode((ReadOnlyMemory<byte>)imageBytes, ReaderLimits.Default);

    internal static ImageXObject Decode(byte[] imageBytes, ReaderLimits limits)
        => Decode((ReadOnlyMemory<byte>)imageBytes, limits);

    internal static ImageXObject Decode(ReadOnlyMemory<byte> imageBytes)
        => Decode(imageBytes, ReaderLimits.Default);

    internal static ImageXObject Decode(ReadOnlyMemory<byte> imageBytes, ReaderLimits limits)
    {
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
        => ImageProbe.DeriveSize(width, height, pixelWidth, pixelHeight);

    internal static (double Width, double Height) Measure(Image image, ImageXObject xobject, double availableWidth)
    {
        var (pixelWidth, pixelHeight) = PixelSize(xobject);
        return ImageProbe.Measure(image, pixelWidth, pixelHeight, availableWidth);
    }

    internal static ImageXObject ApplyOptions(ImageXObject xobject, Image image)
        => ApplyOptions(xobject, image.Interpolate);

    internal static ImageXObject ApplyOptions(ImageXObject xobject, bool interpolate)
    {
        if (interpolate)
        {
            xobject = Copy(xobject);
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
