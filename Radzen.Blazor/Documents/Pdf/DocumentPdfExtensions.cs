using System;
using System.IO;
using System.Threading.Tasks;

namespace Radzen.Documents.Pdf;

/// <summary>
/// Renders <see cref="Document"/> instances to PDF. Importing this namespace makes every document
/// convertible in place; pass a configured <see cref="DocumentRenderer"/> for conformance,
/// accessibility, or metadata settings beyond the defaults.
/// </summary>
public static class DocumentPdfExtensions
{
    /// <summary>Renders the document and returns the complete PDF file bytes.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="renderer">The renderer carrying output settings, or <see langword="null"/> for the defaults.</param>
    /// <returns>The complete PDF file bytes.</returns>
    public static byte[] ToPdf(this Document document, DocumentRenderer? renderer = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        return (renderer ?? new DocumentRenderer()).ToArray(document);
    }

    /// <summary>Renders the document and returns the complete PDF file bytes.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="renderer">The renderer carrying output settings, or <see langword="null"/> for the defaults.</param>
    /// <returns>The complete PDF file bytes.</returns>
    public static ValueTask<byte[]> ToPdfAsync(this Document document, DocumentRenderer? renderer = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        return (renderer ?? new DocumentRenderer()).Render(document).ToArrayAsync();
    }

    /// <summary>Renders the document and writes the PDF to the given stream.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="renderer">The renderer carrying output settings, or <see langword="null"/> for the defaults.</param>
    public static void SaveAsPdf(this Document document, Stream stream, DocumentRenderer? renderer = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        (renderer ?? new DocumentRenderer()).SaveToStream(document, stream);
    }

    /// <summary>Renders the document and writes the PDF to the given stream.</summary>
    /// <param name="document">The document to render.</param>
    /// <param name="stream">The destination stream.</param>
    /// <param name="renderer">The renderer carrying output settings, or <see langword="null"/> for the defaults.</param>
    public static ValueTask SaveAsPdfAsync(this Document document, Stream stream, DocumentRenderer? renderer = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        return (renderer ?? new DocumentRenderer()).Render(document).SaveToStreamAsync(stream);
    }
}
