using System;

namespace Radzen.Documents.Pdf.Signing;

/// <summary>
/// Produces an RFC 3161 time-stamp token over a message hash, for a PAdES
/// document time-stamp (<c>/SubFilter /ETSI.RFC3161</c>, ISO 32000-2 section
/// 12.8.5).
/// </summary>
/// <remarks>
/// This mirrors <see cref="ISigner"/>: the library computes the SHA-256 digest
/// of the bytes covered by the time-stamp's <c>/ByteRange</c> and hands it to
/// the implementation, which runs the Time-Stamping Authority (TSA) round-trip
/// - a network call the library never makes itself, keeping it WASM-safe and
/// deterministic. The implementation returns only the completed DER-encoded
/// <c>TimeStampToken</c> (the CMS <c>ContentInfo</c> from the TSA response's
/// <c>timeStampToken</c> field), which is embedded verbatim in <c>/Contents</c>.
/// </remarks>
public interface ITimestampProvider
{
    /// <summary>
    /// Returns the DER-encoded RFC 3161 <c>TimeStampToken</c> whose message
    /// imprint is <paramref name="hash"/>.
    /// </summary>
    /// <param name="hash">The SHA-256 digest of the bytes covered by the
    /// time-stamp's <c>/ByteRange</c> (the whole file except the
    /// <c>/Contents</c> hex string, angle brackets included).</param>
    /// <returns>The DER-encoded time-stamp token to embed in <c>/Contents</c>.</returns>
    byte[] GetTimestampToken(ReadOnlySpan<byte> hash);
}
