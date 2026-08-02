using System;
using System.Collections.Immutable;
using Radzen.Documents.Core;

namespace Radzen.Documents.Pdf;

internal sealed class ImageDecoders
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

    internal ImageDecoders WithLimits(ResourceLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        return new ImageDecoders(custom, Probes.WithLimits(limits.Snapshot()));
    }

    internal static ImageDecoders BuiltIn { get; } = new([], ImageProbes.None);

    internal ImageDecoders Add(IImageDecoder decoder)
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

    internal ReaderLimits Limits => ReaderLimits.From(Probes.Limits);

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

    internal static Func<ReadOnlyMemory<byte>, ResourceLimits, (double Width, double Height)?> SizeProbe(IImageDecoder decoder)
        => (data, limits) => decoder.TryReadPixelSize(data, ReaderLimits.From(limits), out var width, out var height)
            ? ((double)width, (double)height)
            : null;
}
