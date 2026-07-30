using System;
using System.Collections.Generic;
using Radzen.Documents.Internal;

namespace Radzen.Documents.Pdf.Objects.Filters;

internal static class RunLengthFilter
{
    internal const int Eod = 128;

    // ISO 32000-1 7.4.5: a length byte below 128 is followed by length+1 literal bytes;
    // above 128 by a single byte repeated 257-length times. Returns the packet's total
    // span including its length byte (Eod carries no payload and is handled by the caller).
    internal static int PacketSpan(int lengthByte) => lengthByte < Eod ? lengthByte + 2 : 2;

    public static byte[] Decode(byte[] data) => Decode(data, ReaderLimits.Default.MaxDecodedStreamBytes);

    public static byte[] Decode(byte[] data, long maxOutput)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var output = new PooledBufferStream();
        int i = 0;

        while (i < data.Length)
        {
            int length = data[i++];

            if (length == Eod)
            {
                break;
            }

            if (length < 128)
            {
                int copy = length + 1;
                for (int k = 0; k < copy && i < data.Length; k++)
                {
                    output.WriteByte(data[i++]);
                }
            }
            else
            {
                if (i >= data.Length)
                {
                    break;
                }

                byte value = data[i++];
                int repeat = 257 - length;
                for (int k = 0; k < repeat; k++)
                {
                    output.WriteByte(value);
                }
            }

            if (output.Length > maxOutput)
            {
                throw new DocumentParseException("Decoded stream exceeds the maximum allowed size.", -1);
            }
        }

        return output.ToArray();
    }

    public static byte[] Encode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var output = new List<byte>();
        int i = 0;
        int n = data.Length;

        while (i < n)
        {
            int runLength = 1;
            while (i + runLength < n && data[i + runLength] == data[i] && runLength < 128)
            {
                runLength++;
            }

            if (runLength >= 2)
            {
                output.Add((byte)(257 - runLength));
                output.Add(data[i]);
                i += runLength;
            }
            else
            {
                int start = i;
                int literal = 0;
                while (i < n && literal < 128)
                {
                    if (i + 1 < n && data[i + 1] == data[i])
                    {
                        break;
                    }

                    i++;
                    literal++;
                }

                output.Add((byte)(literal - 1));
                for (int k = 0; k < literal; k++)
                {
                    output.Add(data[start + k]);
                }
            }
        }

        output.Add(Eod);
        return [.. output];
    }
}

internal sealed class RunLengthStreamFilter : IStreamFilter
{
    public string Name => "RunLengthDecode";

    public byte[] Decode(byte[] data, DictionaryObject? parms, long maxOutput)
        => RunLengthFilter.Decode(data, maxOutput);
}
