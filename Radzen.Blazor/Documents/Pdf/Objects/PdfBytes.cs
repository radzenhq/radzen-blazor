using System.IO;

namespace Radzen.Documents.Pdf.Objects;

internal static class PdfBytes
{
    internal static void WriteAscii(Stream stream, string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            stream.WriteByte((byte)text[i]);
        }
    }
}
