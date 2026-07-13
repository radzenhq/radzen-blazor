using System.IO;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// The PDF null object (ISO 32000-1 section 7.3.9), serialized as <c>null</c>.
/// </summary>
public sealed class NullObject : DocumentObject
{
    internal override void Write(Stream stream, WriteContext context)
    {
        PdfBytes.WriteAscii(stream, "null");
    }
}
