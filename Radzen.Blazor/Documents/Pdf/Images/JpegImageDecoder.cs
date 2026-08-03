using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Radzen.Documents.Pdf;

internal sealed class JpegImageDecoder : IImageDecoder
{
    // ISO 32000-1 8.9.5.2: /Decode maps each component onto the given range, low then high.
    private static readonly ImmutableArray<double> InvertedCmyk = [1, 0, 1, 0, 1, 0, 1, 0];

    public bool TryDecode(ReadOnlyMemory<byte> data, ReaderLimits limits, [NotNullWhen(true)] out DecodedImage? image)
    {
        if (!ImageHeaders.IsJpeg(data.Span))
        {
            image = null;
            return false;
        }

        image = DecodeJpeg(data, limits);
        return true;
    }

    private static DecodedImage DecodeJpeg(ReadOnlyMemory<byte> data, ReaderLimits limits)
    {
        var (width, height, precision, components, adobe) = ReadFrame(data.Span);

        limits.ValidateImageDimensions(width, height, "JPEG");

        // ISO 32000-1 8.9.5.1: /BitsPerComponent admits only 1/2/4/8/16.
        if (precision != 8)
        {
            throw new NotSupportedException(
                $"JPEG sample precision {precision} is not supported; PDF images require 8-bit DCTDecode samples.");
        }

        var colorSpace = components switch
        {
            1 => ImageColorSpace.DeviceGray,
            3 => ImageColorSpace.DeviceRgb,
            4 => ImageColorSpace.DeviceCmyk,
            _ => throw new NotSupportedException($"Unsupported JPEG component count {components}."),
        };

        return new DecodedImage(data, width, height, precision, colorSpace)
        {
            Compression = ImageCompression.Jpeg,
            Decode = components == 4 && adobe ? InvertedCmyk : [],
        };
    }

    private static (int Width, int Height, int Precision, int Components, bool Adobe) ReadFrame(ReadOnlySpan<byte> data)
    {
        var position = ImageHeaders.FindJpegFrame(data, out var marker, out var adobe);
        if (marker is not (0xC0 or 0xC1 or 0xC2))
        {
            throw new NotSupportedException(
                $"JPEG start-of-frame marker 0xFF{marker:X2} is not supported by DCTDecode.");
        }

        var frame = ImageHeaders.ReadJpegFrame(data, position);
        return (frame.Width, frame.Height, frame.Precision, frame.Components, adobe);
    }
}
