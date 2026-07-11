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
}
