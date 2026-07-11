using System;
using System.Globalization;
using System.IO;

namespace Radzen.Documents.Pdf.Objects;

internal static class PdfBytes
{
    internal static void WriteAscii(Stream stream, string text) => WriteAscii(stream, text.AsSpan());

    internal static void WriteAscii(Stream stream, ReadOnlySpan<char> text)
    {
        Span<byte> buffer = stackalloc byte[256];
        while (!text.IsEmpty)
        {
            var chunk = Math.Min(text.Length, buffer.Length);
            for (var i = 0; i < chunk; i++)
            {
                buffer[i] = (byte)text[i];
            }

            stream.Write(buffer[..chunk]);
            text = text[chunk..];
        }
    }

    internal static void WriteInteger(Stream stream, long value)
    {
        Span<char> digits = stackalloc char[20];
        value.TryFormat(digits, out var written, provider: CultureInfo.InvariantCulture);
        WriteAscii(stream, digits[..written]);
    }
}
