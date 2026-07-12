using System;
using System.Collections.Generic;
using System.IO;

namespace Radzen.Documents.Pdf.Objects.Filters;

internal static class Ascii85Filter
{
    public static byte[] Decode(byte[] data) => Decode(data, ReaderLimits.Default.MaxDecodedStreamBytes);

    // maxOutput bounds the decoded size for parity with the other filters in a chain.
    public static byte[] Decode(byte[] data, long maxOutput)
    {
        ArgumentNullException.ThrowIfNull(data);

        var output = new List<byte>();
        uint tuple = 0;
        int count = 0;

        foreach (byte b in data)
        {
            if (b == (byte)'~')
            {
                break;
            }

            if (IsWhitespace(b))
            {
                continue;
            }

            if (output.Count > maxOutput)
            {
                throw new DocumentParseException("Decoded stream exceeds the maximum allowed size.", -1);
            }

            if (b == (byte)'z' && count == 0)
            {
                output.Add(0);
                output.Add(0);
                output.Add(0);
                output.Add(0);
                continue;
            }

            if (b < (byte)'!' || b > (byte)'u')
            {
                throw new InvalidDataException($"Invalid ASCII85 character 0x{b:X2}.");
            }

            tuple = tuple * 85 + (uint)(b - '!');
            count++;

            if (count == 5)
            {
                output.Add((byte)(tuple >> 24));
                output.Add((byte)(tuple >> 16));
                output.Add((byte)(tuple >> 8));
                output.Add((byte)tuple);
                tuple = 0;
                count = 0;
            }
        }

        if (count > 0)
        {
            for (int i = count; i < 5; i++)
            {
                tuple = tuple * 85 + 84;
            }

            for (int i = 0; i < count - 1; i++)
            {
                output.Add((byte)(tuple >> (24 - i * 8)));
            }
        }

        return [.. output];
    }

    public static byte[] Encode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var output = new List<byte>();
        int i = 0;

        while (i < data.Length)
        {
            int n = Math.Min(4, data.Length - i);
            uint tuple = 0;
            for (int k = 0; k < 4; k++)
            {
                tuple <<= 8;
                if (k < n)
                {
                    tuple |= data[i + k];
                }
            }

            if (n == 4 && tuple == 0)
            {
                output.Add((byte)'z');
            }
            else
            {
                var group = new byte[5];
                for (int k = 4; k >= 0; k--)
                {
                    group[k] = (byte)('!' + tuple % 85);
                    tuple /= 85;
                }

                for (int k = 0; k < n + 1; k++)
                {
                    output.Add(group[k]);
                }
            }

            i += n;
        }

        output.Add((byte)'~');
        output.Add((byte)'>');
        return [.. output];
    }

    static bool IsWhitespace(byte b) =>
        b is 0 or 9 or 10 or 12 or 13 or 32;
}
