namespace Radzen.Documents.Pdf;

/// <summary>Associates a redaction region with a document page.</summary>
/// <param name="pageIndex">The zero-based page index.</param>
/// <param name="area">The redaction region in PDF user-space coordinates.</param>
public readonly struct PageRedaction(int pageIndex, PdfRect area)
{
    /// <summary>Gets the zero-based page index.</summary>
    public int PageIndex { get; } = pageIndex;

    /// <summary>Gets the redaction region.</summary>
    public PdfRect Area { get; } = area;
}
