using System;
using Radzen.Documents.Core;

namespace Radzen.Documents.Pdf;

internal sealed class ImageDecoders
{
    private static readonly IImageDecoder[] BuiltInDecoders =
    [
        new PngImageDecoder(),
        new JpegImageDecoder(),
        new Jpeg2000ImageDecoder(),
    ];

    private ImageDecoders(ImageProbes probes) => Probes = probes;

    internal ImageDecoders WithLimits(ResourceLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        return new ImageDecoders(Probes.WithLimits(limits.Snapshot()));
    }

    internal static ImageDecoders BuiltIn { get; } = new(ImageProbes.None);

    internal ImageProbes Probes { get; }

    internal ReaderLimits Limits => ReaderLimits.From(Probes.Limits);

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

        throw new NotSupportedException("Unrecognized image format; only PNG, JPEG and JPEG2000 are supported.");
    }
}
