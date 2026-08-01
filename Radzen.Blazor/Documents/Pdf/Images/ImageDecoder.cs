using System;
using System.IO;
using Radzen.Documents.Internal;

namespace Radzen.Documents.Pdf;

/// <summary>
/// The process-wide default set of image decoders. Additional image formats plug in through
/// <see cref="Register(IImageDecoder)"/>, which seeds every <see cref="ImageDecoders"/> set created
/// afterwards; a renderer or document that must not share the process default builds its own set
/// from <see cref="ImageDecoders.BuiltIn"/> instead.
/// </summary>
public static class ImageDecoder
{
    private static volatile ImageDecoders registered = ImageDecoders.BuiltIn;

    private static readonly object RegisterGate = new();

    internal static ImageDecoders Registered => registered;

    /// <summary>
    /// Registers a custom <see cref="IImageDecoder"/> so a new image format can be decoded. The
    /// built-in PNG, JPEG and JPEG2000 decoders are tried first; registered decoders are tried after
    /// them in registration order. The decoder's pixel-size probe travels with it, so a custom
    /// format paginates the same way in every renderer that uses a set seeded from this one.
    /// </summary>
    /// <param name="decoder">The decoder to register.</param>
    public static void Register(IImageDecoder decoder)
    {
        ArgumentNullException.ThrowIfNull(decoder);
        lock (RegisterGate)
        {
            registered = registered.Add(decoder);
        }
    }

    internal static IDisposable Scope() => new RegistryScope();

    internal static byte[] ReadFully(Stream stream) => ReadFully(stream, ReaderLimits.Default);

    internal static byte[] ReadFully(Stream stream, ReaderLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        return StreamBytes.ReadFully(stream, limits.MaxFileBytes);
    }

    internal static DecodedImage Decode(byte[] imageBytes) => Decode((ReadOnlyMemory<byte>)imageBytes, ReaderLimits.Default);

    internal static DecodedImage Decode(byte[] imageBytes, ReaderLimits limits)
        => Decode((ReadOnlyMemory<byte>)imageBytes, limits);

    internal static DecodedImage Decode(ReadOnlyMemory<byte> imageBytes)
        => Decode(imageBytes, ReaderLimits.Default);

    internal static DecodedImage Decode(ReadOnlyMemory<byte> imageBytes, ReaderLimits limits)
        => registered.Decode(imageBytes, limits);

    internal static (double Width, double Height) PixelSize(DecodedImage image) => (image.Width, image.Height);

    internal static (double Width, double Height) Measure(Image image, DecodedImage decoded, double availableWidth)
        => ImageMetrics.Measure(image, decoded.Width, decoded.Height, availableWidth);

    internal static DecodedImage ApplyOptions(DecodedImage image, bool interpolate)
        => interpolate ? image.Interpolated() : image;

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

    private sealed class RegistryScope : IDisposable
    {
        private readonly ImageDecoders decoders = registered;

        public void Dispose()
        {
            lock (RegisterGate)
            {
                registered = decoders;
            }
        }
    }
}
