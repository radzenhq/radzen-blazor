using System;

namespace Radzen.Documents.Pdf.Signing;

/// <summary>
/// The bytes covered by a signature's <c>/ByteRange</c>, presented as the two
/// segments the range names: everything before the <c>/Contents</c> hex string
/// and everything after it. The signed bytes are <see cref="First"/> followed
/// by <see cref="Second"/>, with nothing in between.
/// </summary>
/// <remarks>
/// The segments are views into the document the library already holds, so an
/// <see cref="ISigner"/> that hashes them in order signs a document of any size
/// without a copy. <see cref="ToArray"/> and <see cref="CopyTo"/> are there for
/// implementations that need the covered bytes contiguously - for example to
/// hand them to an API that takes a single array - at the cost of one copy of
/// the whole document.
/// </remarks>
public readonly ref struct SignedContent
{
    internal SignedContent(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second)
    {
        First = first;
        Second = second;
    }

    /// <summary>
    /// Gets the first covered segment: the document up to the <c>/Contents</c>
    /// hex string.
    /// </summary>
    public ReadOnlySpan<byte> First { get; }

    /// <summary>
    /// Gets the second covered segment: the document after the <c>/Contents</c>
    /// hex string. Signed immediately after <see cref="First"/>.
    /// </summary>
    public ReadOnlySpan<byte> Second { get; }

    /// <summary>
    /// Gets the total number of covered bytes.
    /// </summary>
    public int Length => First.Length + Second.Length;

    /// <summary>
    /// Copies the covered bytes, in order, into <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">The span to copy into. Must hold <see cref="Length"/> bytes.</param>
    public void CopyTo(Span<byte> destination)
    {
        First.CopyTo(destination);
        Second.CopyTo(destination[First.Length..]);
    }

    /// <summary>
    /// Returns the covered bytes as a single array. Allocates a copy of the
    /// whole document; prefer consuming <see cref="First"/> and
    /// <see cref="Second"/> in order.
    /// </summary>
    /// <returns>The covered bytes, in order.</returns>
    public byte[] ToArray()
    {
        var content = new byte[Length];
        CopyTo(content);
        return content;
    }
}
