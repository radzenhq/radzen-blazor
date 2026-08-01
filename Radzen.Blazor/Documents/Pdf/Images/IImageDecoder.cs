using System;
using System.Diagnostics.CodeAnalysis;

namespace Radzen.Documents.Pdf;

/// <summary>
/// A pluggable image-format decoder: it sniffs its own magic bytes and, on a match, decodes the
/// payload into samples plus the metadata describing them. Register an implementation with
/// <see cref="ImageDecoder.Register(IImageDecoder)"/>, or add one to a single renderer or document
/// through <see cref="ImageDecoders"/>, so a new format is a new implementation plus a
/// registration, not a central switch edit.
/// </summary>
/// <remarks>
/// Layout never calls <see cref="TryDecode"/>: pagination measures every image through
/// <see cref="TryReadPixelSize"/>, so a decoder that can read its header without decoding the
/// pixels should override it.
/// </remarks>
public interface IImageDecoder
{
    /// <summary>
    /// Attempts to decode <paramref name="data"/>. Returns <c>true</c> and sets
    /// <paramref name="image"/> when the bytes are in this decoder's format; otherwise returns
    /// <c>false</c> and leaves <paramref name="image"/> <c>null</c>.
    /// </summary>
    /// <param name="data">The encoded image bytes, exposed read-only so a decoder cannot mutate the shared payload.</param>
    /// <param name="limits">Resource limits bounding work on malformed or hostile input.</param>
    /// <param name="image">The decoded samples and their metadata on success; otherwise <c>null</c>.</param>
    bool TryDecode(ReadOnlyMemory<byte> data, ReaderLimits limits, [NotNullWhen(true)] out DecodedImage? image);

    /// <summary>
    /// Attempts to read the intrinsic pixel size of <paramref name="data"/> for layout. Returns
    /// <c>true</c> and sets the size when the bytes are in this decoder's format; otherwise returns
    /// <c>false</c>. The default reads the size off a full decode; override it to read the header only.
    /// </summary>
    /// <param name="data">The encoded image bytes.</param>
    /// <param name="limits">Resource limits bounding work on malformed or hostile input.</param>
    /// <param name="width">The width in pixels on success; otherwise <c>0</c>.</param>
    /// <param name="height">The height in pixels on success; otherwise <c>0</c>.</param>
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
