using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Radzen.Documents.Pdf;

internal sealed class Jpeg2000ImageDecoder : IImageDecoder
{
    public bool TryDecode(ReadOnlyMemory<byte> data, ReaderLimits limits, [NotNullWhen(true)] out DecodedImage? image)
    {
        if (!ImageHeaders.IsJpeg2000(data.Span))
        {
            image = null;
            return false;
        }

        image = DecodeJpeg2000(data, limits);
        return true;
    }

    // JPXDecode embeds verbatim with no /ColorSpace, so the JPX stream's own color space applies (ISO 32000-1 7.4.9).
    private static DecodedImage DecodeJpeg2000(ReadOnlyMemory<byte> data, ReaderLimits limits)
    {
        var size = ImageHeaders.ReadJpeg2000Size(data.Span);

        ValidateJpeg2000Dimensions(size.Width, size.Height, size.Components, limits);

        return new DecodedImage(data, size.Width, size.Height, 8, ImageColorSpace.Embedded)
        {
            Compression = ImageCompression.Jpeg2000,
        };
    }

    private static void ValidateJpeg2000Dimensions(int width, int height, int components, ReaderLimits limits)
    {
        if (components <= 0)
        {
            throw new InvalidDataException("JPEG2000 image has invalid dimensions.");
        }

        limits.ValidateImageDimensions(width, height, "JPEG2000");
    }
}
