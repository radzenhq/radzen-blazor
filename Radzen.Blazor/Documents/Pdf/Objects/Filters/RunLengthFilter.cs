using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Objects.Filters;

internal static class RunLengthFilter
{
    const int Eod = 128;

    public static byte[] Decode(byte[] data) => Decode(data, ReaderLimits.Default.MaxDecodedStreamBytes);

    // maxOutput bounds the decoded size; run-length can expand up to 64x, so a small
    // hostile stream aborts with a recoverable DocumentParseException.
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
