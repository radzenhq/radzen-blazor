using System.IO;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// Base class for every object in the PDF Carousel Object System (COS).
/// A <see cref="DocumentObject"/> knows how to serialize itself into the
/// byte grammar defined by ISO 32000-1 section 7.3.
/// </summary>
public abstract class DocumentObject
{
    /// <summary>
    /// Writes the PDF byte representation of this object to <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The destination stream.</param>
    public abstract void Write(Stream stream);
}
