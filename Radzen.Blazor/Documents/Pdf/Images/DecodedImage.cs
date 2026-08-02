using System;
using System.Collections.Immutable;

namespace Radzen.Documents.Pdf;

internal enum ImageColorSpace
{
    Embedded,

    DeviceGray,

    DeviceRgb,

    DeviceCmyk,

    Indexed,
}

internal enum ImageCompression
{
    None,

    Jpeg,

    Jpeg2000,
}

internal sealed class DecodedImage(
    ReadOnlyMemory<byte> samples,
    int width,
    int height,
    int bitsPerComponent,
    ImageColorSpace colorSpace)
{
    internal ReadOnlyMemory<byte> Samples { get; } = samples;

    internal int Width { get; } = width;

    internal int Height { get; } = height;

    internal int BitsPerComponent { get; } = bitsPerComponent;

    internal ImageColorSpace ColorSpace { get; } = colorSpace;

    internal ImageCompression Compression { get; init; }

    internal ReadOnlyMemory<byte> Palette { get; init; }

    // ISO 32000-1 8.9.5.2
    internal ImmutableArray<double> Decode { get; init; }

    // ISO 32000-1 8.9.6.4
    internal ImmutableArray<int> ColorKeyMask { get; init; }

    internal DecodedImage? Alpha { get; init; }

    internal bool Interpolate { get; init; }

    internal DecodedImage Interpolated()
        => new(Samples, Width, Height, BitsPerComponent, ColorSpace)
        {
            Compression = Compression,
            Palette = Palette,
            Decode = Decode,
            ColorKeyMask = ColorKeyMask,
            Alpha = Alpha,
            Interpolate = true,
        };
}
