namespace Radzen.Documents.Pdf.Objects;

// Serializes the "<n> <gen> obj ... endobj" indirect-object framing (ISO 32000-1
// 7.3.10) shared by the full-file and incremental-update writers.
internal static class IndirectObjectFramer
{
    public static long Write(CountingBufferedStream buffer, int number, int generation, DocumentObject value, WriteContext context)
    {
        var offset = buffer.Position;
        PdfBytes.WriteInteger(buffer, number);
        PdfBytes.WriteAscii(buffer, " ");
        PdfBytes.WriteInteger(buffer, generation);
        PdfBytes.WriteAscii(buffer, " obj\n");
        value.Write(buffer, context);
        PdfBytes.WriteAscii(buffer, "\nendobj\n");
        return offset;
    }
}
