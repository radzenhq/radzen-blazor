using System.IO;

namespace Radzen.Documents.Pdf.Objects;

// ISO 32000-1 7.3.9.
internal sealed class NullObject : DocumentObject
{
    internal override void Write(Stream stream, WriteContext context)
    {
        PdfBytes.WriteAscii(stream, "null");
    }
}
