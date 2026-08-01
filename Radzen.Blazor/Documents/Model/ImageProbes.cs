using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Radzen.Documents.Pdf;

namespace Radzen.Documents;

internal sealed class ImageProbes
{
    public static readonly ImageProbes None = new([], ReaderLimits.Default);

    private readonly ImmutableArray<Func<ReadOnlyMemory<byte>, (double Width, double Height)?>> probes;

    private readonly ConditionalWeakTable<byte[], ImageInfo> cache = new();

    private ImageProbes(ImmutableArray<Func<ReadOnlyMemory<byte>, (double Width, double Height)?>> probes, ReaderLimits limits)
    {
        this.probes = probes;
        Limits = limits;
    }

    public int Count => probes.Length;

    public ReaderLimits Limits { get; }

    public ImageProbes Add(Func<ReadOnlyMemory<byte>, (double Width, double Height)?> probe)
    {
        ArgumentNullException.ThrowIfNull(probe);
        return new ImageProbes(probes.Add(probe), Limits);
    }

    public ImageProbes WithLimits(ReaderLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);
        return new ImageProbes(probes, limits);
    }

    public ImageInfo Inspect(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return cache.GetValue(data, Probe);
    }

    public ImageFormat Format(byte[] data) => Inspect(data).Format;

    public (double Width, double Height) PixelSize(byte[] data)
    {
        var info = Inspect(data);
        return (info.Width, info.Height);
    }

    public (double Width, double Height) Measure(Image image, double availableWidth)
    {
        ArgumentNullException.ThrowIfNull(image);

        var pixels = PixelSize(image.Data);
        return ImageMetrics.Measure(image, pixels.Width, pixels.Height, availableWidth);
    }

    public (double Width, double Height) Measure(InlineImage image)
    {
        ArgumentNullException.ThrowIfNull(image);

        var pixels = PixelSize(image.Data);
        return ImageMetrics.DeriveSize(image.Width, image.Height, pixels.Width, pixels.Height);
    }

    private ImageInfo Probe(byte[] data)
    {
        if (ImageHeaders.IsPng(data))
        {
            var header = ImageHeaders.ReadPngHeader(data);
            return Validate(header.Width, header.Height, ImageFormat.Png);
        }

        if (ImageHeaders.IsJpeg(data))
        {
            return ReadJpeg(data);
        }

        if (ImageHeaders.IsJpeg2000(data))
        {
            var size = ImageHeaders.ReadJpeg2000Size(data);
            return Validate(size.Width, size.Height, ImageFormat.Jpeg2000);
        }

        foreach (var probe in probes)
        {
            if (probe(data) is { } size)
            {
                return Validate((long)size.Width, (long)size.Height, ImageFormat.Custom);
            }
        }

        throw new NotSupportedException("Unrecognized image format; only PNG, JPEG and JPEG2000 are supported.");
    }

    private ImageInfo ReadJpeg(byte[] data)
    {
        var position = ImageHeaders.FindJpegFrame(data, out var marker, out _);
        if (marker is not (0xC0 or 0xC1 or 0xC2))
        {
            throw new NotSupportedException(
                $"JPEG start-of-frame marker 0xFF{marker:X2} is not supported.");
        }

        var frame = ImageHeaders.ReadJpegFrame(data, position);
        return Validate(frame.Width, frame.Height, ImageFormat.Jpeg);
    }

    private ImageInfo Validate(long width, long height, ImageFormat format)
    {
        ImageDecoder.ValidateImageDimensions(width, height, Limits, FormatName(format));
        return new ImageInfo(format, width, height);
    }

    private static string FormatName(ImageFormat format) => format switch
    {
        ImageFormat.Png => "PNG",
        ImageFormat.Jpeg => "JPEG",
        ImageFormat.Jpeg2000 => "JPEG2000",
        _ => "Custom-format",
    };
}
