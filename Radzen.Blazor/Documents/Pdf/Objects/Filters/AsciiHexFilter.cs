using System;
using System.IO;

namespace Radzen.Documents.Pdf.Objects.Filters;

internal static class AsciiHexFilter
{
    public static byte[] Decode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var position = 0;

        try
        {
            return Lexer.ReadHexDigits(data, ref position, Lexer.Recovery.Strict, Lexer.HexEnd.Optional);
        }
        catch (DocumentParseException e)
        {
            // The Filters layer reports a corrupt payload as InvalidDataException (see Ascii85Filter).
            throw new InvalidDataException($"Invalid ASCIIHex character 0x{data[e.Offset]:X2}.", e);
        }
    }

    public static byte[] Encode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        // Two hex digits per byte plus the '>' terminator must fit a single array.
        var maxEncodable = (Array.MaxLength - 1) / 2;

        if (data.Length > maxEncodable)
        {
            throw new ArgumentException(
                $"Cannot ASCIIHex-encode {data.Length} bytes: the encoded output exceeds the maximum array length. The limit is {maxEncodable} bytes.",
                nameof(data));
        }

        var output = new byte[data.Length * 2 + 1];
        HexCodec.Encode(data, output, HexCase.Upper);
        output[^1] = (byte)'>';
        return output;
    }
}

internal sealed class AsciiHexStreamFilter : IStreamFilter
{
    public string Name => "ASCIIHexDecode";

    public byte[] Decode(byte[] data, DictionaryObject? parms, long maxOutput)
        => AsciiHexFilter.Decode(data);
}
