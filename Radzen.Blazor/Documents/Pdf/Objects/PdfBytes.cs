using System.IO;

namespace Radzen.Documents.Pdf.Objects;

/// <summary>
/// Helpers for emitting PDF syntax as Latin-1/ASCII bytes. PDF syntax tokens
/// are single-byte; never UTF-8 encode them.
/// </summary>
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
