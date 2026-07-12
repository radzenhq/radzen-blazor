using System;

namespace Radzen.Documents.Pdf.Signing;

/// <summary>
/// Produces a detached PKCS#7/CMS signature over the bytes of a PDF document
/// covered by a signature's <c>/ByteRange</c>.
/// </summary>
/// <remarks>
/// Implementations are responsible for hashing the content, signing with a
/// private key, and assembling the DER-encoded CMS <c>SignedData</c>
/// structure. This library never sees a private key and performs no
/// public-key cryptography itself - it is safe to ship to the browser
/// (Blazor WebAssembly). A production implementation runs the actual signing
/// server-side (web API, HSM, KMS, ...) and returns only the completed CMS
/// blob. The library always passes the raw covered bytes; implementations may
/// hash them by any means - for example with the pure-managed
/// <c>Radzen.Documents.Crypto.Sha2.Sha256</c> - before forwarding the digest
/// to a remote signing service.
/// </remarks>
public interface ISigner
{
    /// <summary>
    /// Signs the exact bytes covered by the signature's <c>/ByteRange</c> -
    /// the whole file except the <c>/Contents</c> hex string (including its
    /// angle brackets).
    /// </summary>
    /// <param name="content">The bytes to sign.</param>
    /// <returns>A DER-encoded detached PKCS#7/CMS <c>SignedData</c>.</returns>
    byte[] Sign(ReadOnlySpan<byte> content);
}
