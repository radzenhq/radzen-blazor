using System;
using System.Diagnostics.CodeAnalysis;

namespace Radzen.Documents.Pdf;

internal interface IImageDecoder
{
    bool TryDecode(ReadOnlyMemory<byte> data, ReaderLimits limits, [NotNullWhen(true)] out DecodedImage? image);
}
