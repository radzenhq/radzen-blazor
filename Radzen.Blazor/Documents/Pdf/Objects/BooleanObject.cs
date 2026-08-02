using System.IO;

namespace Radzen.Documents.Pdf.Objects;

// ISO 32000-1 7.3.2.
internal sealed class BooleanObject(bool value) : DocumentObject
{
    public bool Value { get; } = value;

    internal override void Write(Stream stream, WriteContext context)
    {
        PdfBytes.WriteAscii(stream, Value ? "true" : "false");
    }
}
