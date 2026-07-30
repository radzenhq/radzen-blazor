using System;
using System.Collections.Immutable;

namespace Radzen.Documents.Pdf;

/// <summary>
/// How the components of a decoded sample are interpreted.
/// </summary>
public enum ImageColorSpace
{
    /// <summary>
    /// The samples carry their own color space, as a JPEG2000 codestream does. No color space is
    /// written and the viewer takes it from the sample data.
    /// </summary>
    Embedded,

    /// <summary>One grayscale component per sample.</summary>
    DeviceGray,

    /// <summary>Three components per sample: red, green, then blue.</summary>
    DeviceRgb,

    /// <summary>Four components per sample: cyan, magenta, yellow, then black.</summary>
    DeviceCmyk,

    /// <summary>
    /// One index per sample into <see cref="DecodedImage.Palette"/>, which holds the RGB triples
    /// the indices select.
    /// </summary>
    Indexed,
}

/// <summary>
/// The compression <see cref="DecodedImage.Samples"/> already carry.
/// </summary>
public enum ImageCompression
{
    /// <summary>Uncompressed samples, packed row by row. The writer compresses them.</summary>
    None,

    /// <summary>A JPEG (DCT) datastream embedded verbatim, decoded by the viewer.</summary>
    Jpeg,

    /// <summary>A JPEG2000 codestream embedded verbatim, decoded by the viewer.</summary>
    Jpeg2000,
}

/// <summary>
/// The samples an <see cref="IImageDecoder"/> produces plus the metadata needed to interpret them:
/// pixel size, sample depth, color space, the compression the samples already carry, and the
/// optional palette, decode array, color key and alpha channel.
/// </summary>
/// <param name="samples">
/// The sample data: raw rows when <see cref="Compression"/> is <see cref="ImageCompression.None"/>,
/// otherwise the compressed datastream verbatim.
/// </param>
/// <param name="width">The width in pixels.</param>
/// <param name="height">The height in pixels.</param>
/// <param name="bitsPerComponent">The bits each component of a sample occupies.</param>
/// <param name="colorSpace">How the components of a sample are interpreted.</param>
public sealed class DecodedImage(
    ReadOnlyMemory<byte> samples,
    int width,
    int height,
    int bitsPerComponent,
    ImageColorSpace colorSpace)
{
    /// <summary>Gets the sample data.</summary>
    public ReadOnlyMemory<byte> Samples { get; } = samples;

    /// <summary>Gets the width in pixels.</summary>
    public int Width { get; } = width;

    /// <summary>Gets the height in pixels.</summary>
    public int Height { get; } = height;

    /// <summary>Gets the bits each component of a sample occupies.</summary>
    public int BitsPerComponent { get; } = bitsPerComponent;

    /// <summary>Gets how the components of a sample are interpreted.</summary>
    public ImageColorSpace ColorSpace { get; } = colorSpace;

    /// <summary>Gets the compression <see cref="Samples"/> already carry. Defaults to <see cref="ImageCompression.None"/>.</summary>
    public ImageCompression Compression { get; init; }

    /// <summary>
    /// Gets the palette an <see cref="ImageColorSpace.Indexed"/> image indexes into, as one RGB
    /// triple per entry and at most 256 entries. Empty for every other color space.
    /// </summary>
    public ReadOnlyMemory<byte> Palette { get; init; }

    /// <summary>
    /// Gets the range each component is mapped onto, as a minimum and a maximum per component
    /// (ISO 32000-1 8.9.5.2). Empty leaves the samples unmapped; <c>[1, 0, 1, 0, 1, 0, 1, 0]</c>
    /// inverts a four-component image.
    /// </summary>
    public ImmutableArray<double> Decode { get; init; }

    /// <summary>
    /// Gets the sample values painted as transparent, as an inclusive minimum and maximum per
    /// component (ISO 32000-1 8.9.6.4). Empty when the image has no color key.
    /// </summary>
    public ImmutableArray<int> ColorKeyMask { get; init; }

    /// <summary>
    /// Gets the alpha channel, an <see cref="ImageColorSpace.DeviceGray"/> image of the same pixel
    /// size whose samples give the opacity of the matching pixel. <c>null</c> when the image is opaque.
    /// </summary>
    public DecodedImage? Alpha { get; init; }

    /// <summary>Gets whether a viewer smooths the image when it is scaled up.</summary>
    public bool Interpolate { get; init; }

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
