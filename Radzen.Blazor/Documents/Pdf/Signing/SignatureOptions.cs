using System;

namespace Radzen.Documents.Pdf.Signing;

/// <summary>
/// Options for <see cref="PdfSigner.Sign(byte[], SignatureOptions, ISigner)"/>.
/// </summary>
public sealed class SignatureOptions
{
    /// <summary>
    /// Gets or sets the reason for signing, written as <c>/Reason</c> in the
    /// signature dictionary when set.
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Gets or sets the CPU host name or physical location of signing,
    /// written as <c>/Location</c> when set.
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Gets or sets contact information for the signer, written as
    /// <c>/ContactInfo</c> when set.
    /// </summary>
    public string? ContactInfo { get; set; }

    /// <summary>
    /// Gets or sets the name of the person or authority signing the document,
    /// written as <c>/Name</c> in the signature dictionary when set.
    /// </summary>
    public string? SignerName { get; set; }

    /// <summary>
    /// Gets or sets the time of signing. The library never reads the clock -
    /// the caller provides the time, keeping the output deterministic. When
    /// set it is converted to UTC and written as <c>/M</c> in PDF date form
    /// <c>D:yyyyMMddHHmmss+00'00'</c>.
    /// </summary>
    public DateTimeOffset? SigningTime { get; set; }

    /// <summary>
    /// Gets or sets the size in raw bytes (before hex encoding) reserved for
    /// the <c>/Contents</c> placeholder the signer's CMS blob must fit into.
    /// Raise it when the signer embeds long certificate chains or timestamps.
    /// </summary>
    public int SignatureMaxSizeBytes { get; set; } = 16384;

    /// <summary>
    /// Gets or sets the <c>/SubFilter</c> name describing the encoding of the
    /// signature value. <c>adbe.pkcs7.detached</c> (the default) is the
    /// classic detached PKCS#7 encoding; use <c>ETSI.CAdES.detached</c> for
    /// PAdES signatures.
    /// </summary>
    public string SubFilter { get; set; } = "adbe.pkcs7.detached";
}
