namespace Radzen.Documents.Pdf;

/// <summary>
/// PDF/UA accessibility conformance level applied when saving a built document.
/// </summary>
public enum PdfUaConformance
{
    /// <summary>No PDF/UA conformance; the output is not identified as accessible.</summary>
    None,

    /// <summary>PDF/UA-1 (ISO 14289-1).</summary>
    PdfUa1,
}
