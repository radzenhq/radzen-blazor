using System;
using System.Diagnostics.CodeAnalysis;

namespace Radzen.Documents.Pdf;

/// <summary>
/// A pluggable image-format decoder: it sniffs its own magic bytes and, on a match, decodes
/// the payload into an image XObject. Register an implementation with
/// <see cref="ImageDecoder.Register(IImageDecoder)"/> so a new format is a new implementation
/// plus a registration, not a central switch edit.
/// </summary>
/// <remarks>
/// This is a PDF emission extension: it produces the PDF resource an image is painted from.
/// Layout never calls it - pagination measures every image through the renderer-neutral size
/// probe, which <see cref="ImageDecoder.Register(IImageDecoder)"/> bridges to this decoder so
/// both paths agree on the intrinsic pixel size.
/// </remarks>
public interface IImageDecoder
{
    /// <summary>
    /// Attempts to decode <paramref name="data"/>. Returns <c>true</c> and sets
    /// <paramref name="xobject"/> when the bytes are in this decoder's format; otherwise returns
    /// <c>false</c> and leaves <paramref name="xobject"/> <c>null</c>.
    /// </summary>
    /// <param name="data">The encoded image bytes, exposed read-only so a decoder cannot mutate the shared payload.</param>
    /// <param name="limits">Resource limits bounding work on malformed or hostile input.</param>
    /// <param name="xobject">The decoded image resource on success; otherwise <c>null</c>.</param>
    bool TryDecode(ReadOnlyMemory<byte> data, ReaderLimits limits, [NotNullWhen(true)] out ImageXObject? xobject);
}
