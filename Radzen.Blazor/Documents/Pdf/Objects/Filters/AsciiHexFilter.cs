using System;
using System.IO;

namespace Radzen.Documents.Pdf.Objects.Filters;

internal static class AsciiHexFilter
{
    public static byte[] Decode(byte[] data, long maxOutput)
    {
        ArgumentNullException.ThrowIfNull(data);

        EnsureDecodedSizeWithinLimit(data, maxOutput);

        var position = 0;

        try
        {
            return Lexer.ReadHexDigits(data, ref position, Lexer.Recovery.Strict, Lexer.HexEnd.Optional);
        }
        catch (DocumentParseException e)
        {
            throw new InvalidDataException($"Invalid ASCIIHex character 0x{data[e.Offset]:X2}.", e);
        }
    }

    private static void EnsureDecodedSizeWithinLimit(byte[] data, long maxOutput)
    {
        long digits = 0;
        foreach (var b in data)
        {
            if (b == (byte)'>')
            {
                break;
            }

            if (!Lexer.IsWhitespace(b))
            {
                digits++;
            }
        }

        if ((digits + 1) / 2 > maxOutput)
        {
            throw new DocumentParseException("Decoded stream exceeds the maximum allowed size.", -1);
        }
    }
}

internal sealed class AsciiHexStreamFilter : IStreamFilter
{
    public string Name => "ASCIIHexDecode";

    public byte[] Decode(byte[] data, DictionaryObject? parms, long maxOutput)
        => AsciiHexFilter.Decode(data, maxOutput);
}
