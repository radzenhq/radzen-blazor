namespace Radzen.Documents.Pdf;

/// <summary>
/// PDF/A conformance level applied when saving a built document.
/// </summary>
public enum PdfAConformance
{
    /// <summary>No PDF/A conformance; plain PDF output.</summary>
    None,

    /// <summary>PDF/A-3 Level B (visual reproducibility).</summary>
    PdfA3B,

    /// <summary>PDF/A-3 Level A (Level B plus Tagged PDF logical structure).</summary>
    PdfA3A,

    /// <summary>PDF/A-2 Level B (visual reproducibility). Only PDF/A conformant embedded files are permitted, which this library cannot verify, so attachments are rejected.</summary>
    PdfA2B,

    /// <summary>PDF/A-2 Level A (Level B plus Tagged PDF logical structure). Only PDF/A conformant embedded files are permitted, which this library cannot verify, so attachments are rejected.</summary>
    PdfA2A,

    /// <summary>PDF/A-4 (ISO 19005-4:2020). Identified with pdfaid:part 4 and no conformance letter. Only PDF/A conformant embedded files are permitted, which this library cannot verify, so attachments are rejected.</summary>
    PdfA4,

    /// <summary>PDF/A-4 Level E (engineering). Attachments are rejected as under <see cref="PdfA4"/>.</summary>
    PdfA4E,

    /// <summary>PDF/A-4 Level F. Requires at least one embedded file (attachment) of any type.</summary>
    PdfA4F,
}
