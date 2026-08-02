using System;
using System.Diagnostics.CodeAnalysis;

namespace Radzen.Documents.Pdf;

internal interface IImageDecoder
{
    bool TryDecode(ReadOnlyMemory<byte> data, ReaderLimits limits, [NotNullWhen(true)] out DecodedImage? image);

    bool TryReadPixelSize(ReadOnlyMemory<byte> data, ReaderLimits limits, out int width, out int height)
    {
        if (TryDecode(data, limits, out var image))
        {
            width = image.Width;
            height = image.Height;
            return true;
        }

        width = 0;
        height = 0;
        return false;
    }
}
