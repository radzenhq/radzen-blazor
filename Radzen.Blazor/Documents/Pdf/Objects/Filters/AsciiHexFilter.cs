using System;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Pdf.Objects.Filters;

internal static class AsciiHexFilter
{
    const string HexDigits = "0123456789ABCDEF";

    public static byte[] Decode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var output = new List<byte>();
        int high = -1;

        foreach (byte b in data)
        {
            if (b == (byte)'>')
            {
                break;
            }

            if (IsWhitespace(b))
            {
                continue;
            }

            int value = HexValue(b);
            if (value < 0)
            {
                throw new InvalidDataException($"Invalid ASCIIHex character 0x{b:X2}.");
            }

            if (high < 0)
            {
                high = value;
            }
            else
            {
                output.Add((byte)((high << 4) | value));
                high = -1;
            }
        }

        if (high >= 0)
        {
            output.Add((byte)(high << 4));
        }

        return [.. output];
    }

    public static byte[] Encode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var output = new byte[data.Length * 2 + 1];
        int j = 0;

        foreach (byte b in data)
        {
            output[j++] = (byte)HexDigits[b >> 4];
            output[j++] = (byte)HexDigits[b & 0x0F];
        }

        output[j] = (byte)'>';
        return output;
    }

    static bool IsWhitespace(byte b) =>
        b is 0 or 9 or 10 or 12 or 13 or 32;

    static int HexValue(byte b) => b switch
    {
        >= (byte)'0' and <= (byte)'9' => b - '0',
        >= (byte)'A' and <= (byte)'F' => b - 'A' + 10,
        >= (byte)'a' and <= (byte)'f' => b - 'a' + 10,
        _ => -1,
    };
}
