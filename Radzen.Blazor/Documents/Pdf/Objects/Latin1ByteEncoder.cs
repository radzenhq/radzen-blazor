using System;

namespace Radzen.Documents.Pdf.Objects;

internal static class Latin1ByteEncoder
{
    internal static void Encode(ReadOnlySpan<char> text, Span<byte> destination)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var value = text[i];
            if (value > 0xFF)
            {
                throw new ArgumentOutOfRangeException(nameof(text), value, "A byte-oriented PDF token cannot encode a character outside the Latin-1 range.");
            }

            destination[i] = (byte)value;
        }
    }
}
