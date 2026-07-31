using System;
using System.Collections.Immutable;

namespace Radzen.Documents.Pdf.Emission;

internal sealed class EmissionImagePayload
{
    private readonly byte[] samples;

    private readonly byte[] palette;

    private EmissionImagePayload(DecodedImage image)
    {
        samples = image.Samples.ToArray();
        palette = image.Palette.ToArray();
        Width = image.Width;
        Height = image.Height;
        BitsPerComponent = image.BitsPerComponent;
        ColorSpace = image.ColorSpace;
        Compression = image.Compression;
        Decode = image.Decode.IsDefault ? [] : image.Decode;
        ColorKeyMask = image.ColorKeyMask.IsDefault ? [] : image.ColorKeyMask;
        Interpolate = image.Interpolate;
    }

    public static EmissionImagePayload Capture(DecodedImage image) => new(image);

    public ReadOnlySpan<byte> Samples => samples;

    public ReadOnlySpan<byte> Palette => palette;

    public int Width { get; }

    public int Height { get; }

    public int BitsPerComponent { get; }

    public ImageColorSpace ColorSpace { get; }

    public ImageCompression Compression { get; }

    public ImmutableArray<double> Decode { get; }

    public ImmutableArray<int> ColorKeyMask { get; }

    public bool Interpolate { get; }
}
