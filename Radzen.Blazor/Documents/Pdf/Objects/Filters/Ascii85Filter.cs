using System;
using System.IO;
using Radzen.Documents.Internal;

namespace Radzen.Documents.Pdf.Objects.Filters;

internal static class Ascii85Filter
{
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

            if (Lexer.IsWhitespace(b))
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
}

internal sealed class Ascii85StreamFilter : IStreamFilter
{
    public string Name => "ASCII85Decode";

    public byte[] Decode(byte[] data, DictionaryObject? parms, long maxOutput)
        => Ascii85Filter.Decode(data, maxOutput);
}
