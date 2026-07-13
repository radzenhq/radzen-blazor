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

        using var output = new PooledBufferStream();
        ulong tuple = 0;
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

            if (output.Length > maxOutput)
            {
                throw new DocumentParseException("Decoded stream exceeds the maximum allowed size.", -1);
            }

            if (b == (byte)'z' && count == 0)
            {
                output.WriteByte(0);
                output.WriteByte(0);
                output.WriteByte(0);
                output.WriteByte(0);
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
                // A 5-tuple encodes a 32-bit value; a group like "s8W-#" exceeds 0xFFFFFFFF.
                // Fail loud rather than silently truncating to the low 32 bits.
                if (tuple > uint.MaxValue)
                {
                    throw new InvalidDataException("ASCII85 5-tuple exceeds the 32-bit maximum.");
                }

                output.WriteByte((byte)(tuple >> 24));
                output.WriteByte((byte)(tuple >> 16));
                output.WriteByte((byte)(tuple >> 8));
                output.WriteByte((byte)tuple);
                tuple = 0;
                count = 0;
            }
        }

        if (count > 0)
        {
            // A final group of a single character cannot encode any bytes (it would emit
            // count-1 == 0 bytes); a truncated stream ending in one stray char is corrupt.
            if (count == 1)
            {
                throw new InvalidDataException("ASCII85 stream ends with a dangling single character.");
            }

            for (int i = count; i < 5; i++)
            {
                tuple = tuple * 85 + 84;
            }

            for (int i = 0; i < count - 1; i++)
            {
                output.WriteByte((byte)(tuple >> (24 - i * 8)));
            }
        }

        return output.ToArray();
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
