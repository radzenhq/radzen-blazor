using System;
using System.Collections.Immutable;

namespace Radzen.Documents.Pdf;

/// <summary>
/// An immutable set of image decoders. The built-in PNG, JPEG and JPEG2000 decoders are tried
/// first, then the added ones in the order they were added. Add a decoder to
/// <see cref="DocumentRenderer.ImageDecoders"/> or <see cref="PortableDocument.ImageDecoders"/> to
/// reach a custom format from one renderer or document only; register it with
/// <see cref="ImageDecoder.Register(IImageDecoder)"/> to seed every set created afterwards.
/// </summary>
public sealed class ImageDecoders
{
    private const int CustomDecoderLimit = 64;

    private static readonly IImageDecoder[] BuiltInDecoders =
    [
        new PngImageDecoder(),
        new JpegImageDecoder(),
        new Jpeg2000ImageDecoder(),
    ];

    private readonly ImmutableArray<IImageDecoder> custom;

    private ImageDecoders(ImmutableArray<IImageDecoder> custom, ImageProbes probes)
    {
        this.custom = custom;
        Probes = probes;
    }

    /// <summary>
    /// Returns a set holding this set's decoders bounded by <paramref name="limits"/>. The same
    /// limits govern both paths an image travels: the pixel-size measurement layout paginates
    /// from, and the decoding the renderer performs.
    /// </summary>
    /// <param name="limits">The resource limits to decode and measure with.</param>
    /// <returns>A set with the given limits.</returns>
    public ImageDecoders WithLimits(ReaderLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        return new ImageDecoders(custom, Probes.WithLimits(limits.Snapshot()));
    }

    /// <summary>Gets the set holding only the built-in PNG, JPEG and JPEG2000 decoders.</summary>
    public static ImageDecoders BuiltIn { get; } = new([], ImageProbes.None);

    /// <summary>Gets the set every renderer and document starts from: the built-in decoders plus
    /// the ones registered with <see cref="ImageDecoder.Register(IImageDecoder)"/> so far.</summary>
    public static ImageDecoders Default => ImageDecoder.Registered;

    /// <summary>
    /// Returns a set holding this set's decoders plus <paramref name="decoder"/>, tried last.
    /// Returns this set unchanged when it already holds <paramref name="decoder"/>.
    /// </summary>
    /// <param name="decoder">The decoder to add.</param>
    /// <exception cref="InvalidOperationException">The set already holds the maximum number of custom decoders.</exception>
    public ImageDecoders Add(IImageDecoder decoder)
    {
        ArgumentNullException.ThrowIfNull(decoder);
        if (Contains(decoder))
        {
            return this;
        }

        if (custom.Length >= CustomDecoderLimit)
        {
            throw new InvalidOperationException($"No more than {CustomDecoderLimit} custom image decoders can be registered.");
        }

        return new ImageDecoders(custom.Add(decoder), Probes.Add(SizeProbe(decoder)));
    }

    internal ImageProbes Probes { get; }

    internal ReaderLimits Limits => Probes.Limits;

    internal bool Contains(IImageDecoder decoder)
    {
        foreach (var existing in custom)
        {
            if (ReferenceEquals(existing, decoder))
            {
                return true;
            }
        }

        return false;
    }

    internal DecodedImage Decode(ReadOnlyMemory<byte> data) => Decode(data, Limits);

    internal DecodedImage Decode(ReadOnlyMemory<byte> data, ReaderLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        foreach (var decoder in BuiltInDecoders)
        {
            if (decoder.TryDecode(data, limits, out var image))
            {
                return image;
            }
        }

        foreach (var decoder in custom)
        {
            if (decoder.TryDecode(data, limits, out var image))
            {
                return image;
            }
        }

        throw new NotSupportedException("Unrecognized image format; only PNG, JPEG and JPEG2000 are supported.");
    }

    internal static Func<ReadOnlyMemory<byte>, (double Width, double Height)?> SizeProbe(IImageDecoder decoder)
        => data => decoder.TryReadPixelSize(data, out var width, out var height)
            ? ((double)width, (double)height)
            : null;
}
