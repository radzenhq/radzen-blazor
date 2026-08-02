using System.IO;

namespace Radzen.Documents.Pdf.Objects;

// ISO 32000-1 7.3.10.
internal sealed class ReferenceObject(int objectNumber, int generation) : DocumentObject
{
    public int ObjectNumber { get; } = objectNumber;

    public int Generation { get; } = generation;

    internal override void Write(Stream stream, WriteContext context)
    {
        PdfBytes.WriteInteger(stream, ObjectNumber);
        stream.WriteByte((byte)' ');
        PdfBytes.WriteInteger(stream, Generation);
        PdfBytes.WriteAscii(stream, " R");
    }
}
