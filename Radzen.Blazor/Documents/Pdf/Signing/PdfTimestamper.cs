using System;
using Radzen.Documents.Crypto;
using Radzen.Documents.Pdf.Objects;

namespace Radzen.Documents.Pdf.Signing;

/// <summary>
/// Adds a PAdES document time-stamp (the B-T building block, a
/// <c>DocTimeStamp</c> per ISO 32000-2 section 12.8.5) to an existing PDF as an
/// incremental update: a signature field whose value dictionary carries
/// <c>/Type /DocTimeStamp</c>, <c>/SubFilter /ETSI.RFC3161</c>, a <c>/ByteRange</c> over
/// the whole file except <c>/Contents</c>, and an RFC 3161 time-stamp token in
/// <c>/Contents</c>.
/// </summary>
/// <remarks>
/// The token comes from a caller-supplied <see cref="ITimestampProvider"/> - the
/// library computes the SHA-256 of the covered bytes with the pure-managed
/// <see cref="Sha2"/> and delegates the Time-Stamping Authority round-trip, so
/// it never talks to the network and stays WASM-safe. Output is deterministic:
/// identical inputs and provider output produce identical bytes. The original
/// bytes are an exact prefix of the result. Stacking a document time-stamp on
/// top of a B-LT document (see <see cref="DssBuilder"/>) yields a B-LTA
/// (long-term archival) document.
/// </remarks>
public static class PdfTimestamper
{
    private const int DefaultReservedBytes = 16384;

    /// <summary>
    /// Time-stamps <paramref name="pdf"/> reserving the default 16 KB for the
    /// token, and returns the augmented document.
    /// </summary>
    /// <param name="pdf">The complete bytes of the document to time-stamp.</param>
    /// <param name="provider">Produces the RFC 3161 token. See <see cref="ITimestampProvider"/>.</param>
    /// <returns>The bytes of the time-stamped document.</returns>
    public static byte[] Timestamp(byte[] pdf, ITimestampProvider provider)
        => Timestamp(pdf, provider, DefaultReservedBytes);

    /// <summary>
    /// Time-stamps <paramref name="pdf"/> and returns the augmented document.
    /// </summary>
    /// <param name="pdf">The complete bytes of the document to time-stamp.</param>
    /// <param name="provider">Produces the RFC 3161 token. See <see cref="ITimestampProvider"/>.</param>
    /// <param name="reservedBytes">The size in raw bytes (before hex encoding)
    /// reserved for the token in <c>/Contents</c>. Raise it for TSAs that embed
    /// long certificate chains.</param>
    /// <returns>The bytes of the time-stamped document.</returns>
    /// <exception cref="InvalidOperationException">The provider returned a token
    /// larger than <paramref name="reservedBytes"/>.</exception>
    public static byte[] Timestamp(byte[] pdf, ITimestampProvider provider, int reservedBytes)
    {
        ArgumentNullException.ThrowIfNull(pdf);
        ArgumentNullException.ThrowIfNull(provider);
        if (reservedBytes < 1 || reservedBytes > PdfSigner.MaxReservation)
        {
            throw new ArgumentOutOfRangeException(nameof(reservedBytes), reservedBytes,
                $"reservedBytes must be between 1 and {PdfSigner.MaxReservation}.");
        }

        var signature = new DictionaryObject
        {
            ["Type"] = new NameObject("DocTimeStamp"),
            ["Filter"] = new NameObject("Adobe.PPKLite"),
            ["SubFilter"] = new NameObject("ETSI.RFC3161"),
            ["ByteRange"] = PdfSigner.ByteRangePlaceholder(),
            ["Contents"] = PdfSigner.ContentsPlaceholder(reservedBytes),
        };

        var (bytes, sigStart, sigEnd) = PdfSigner.AppendSignatureField(
            pdf, signature, appearanceStream: null,
            rect: [new NumberObject(0), new NumberObject(0), new NumberObject(0), new NumberObject(0)],
            pageIndex: 0);

        return PdfSigner.Embed(bytes, sigStart, sigEnd, reservedBytes,
            content => provider.GetTimestampToken(Sha2.ComputeHash256(content.ToArray()))
                ?? throw new InvalidOperationException("The timestamp provider returned null."),
            "Increase the reservedBytes argument.");
    }
}
