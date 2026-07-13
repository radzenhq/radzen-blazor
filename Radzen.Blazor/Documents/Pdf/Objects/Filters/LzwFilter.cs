using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Objects.Filters;

internal static class LzwFilter
{
    const int Clear = 256;
    const int Eod = 257;

    public static byte[] Decode(byte[] data, int early)
        => Decode(data, early, ReaderLimits.Default.MaxDecodedStreamBytes);

    // maxOutput bounds the decoded size against an LZW bomb; codes are bounds-checked
    // against the table so a code past the current dictionary raises a recoverable
    // DocumentParseException instead of an IndexOutOfRangeException.
    //
    // The dictionary stores each entry as (prefix code, appended byte, first byte, length)
    // rather than a full byte[] so growing the table never copies whole sequences; an entry
    // is materialized only when it is emitted, by walking its prefix chain.
    public static byte[] Decode(byte[] data, int early, long maxOutput)
    {
        ArgumentNullException.ThrowIfNull(data);

        using var output = new PooledBufferStream();
        if (data.Length == 0)
        {
            return output.ToArray();
        }

        var prefix = new List<int>();
        var append = new List<byte>();
        var first = new List<byte>();
        var length = new List<int>();

        void ResetTable()
        {
            prefix.Clear();
            append.Clear();
            first.Clear();
            length.Clear();
            for (int i = 0; i < 256; i++)
            {
                prefix.Add(-1);
                append.Add((byte)i);
                first.Add((byte)i);
                length.Add(1);
            }

            // Clear (256) and Eod (257) placeholders; never emitted as sequences.
            for (int i = 0; i < 2; i++)
            {
                prefix.Add(-1);
                append.Add(0);
                first.Add(0);
                length.Add(0);
            }
        }

        void AddEntry(int prefixCode, byte firstByte)
        {
            prefix.Add(prefixCode);
            append.Add(firstByte);
            first.Add(first[prefixCode]);
            length.Add(length[prefixCode] + 1);
        }

        byte[] scratch = [];
        void WriteEntry(int code)
        {
            int len = length[code];
            if (scratch.Length < len)
            {
                scratch = new byte[len];
            }

            int p = code;
            for (int idx = len - 1; idx >= 0; idx--)
            {
                scratch[idx] = append[p];
                p = prefix[p];
            }

            output.Write(scratch, 0, len);
        }

        ResetTable();
        int width = 9;
        int nextCode = 258;
        int previous = -1;
        int bitPos = 0;

        int totalBits = data.Length * 8;

        while (bitPos + width <= totalBits)
        {
            int code = 0;
            for (int i = 0; i < width; i++)
            {
                int bit = (data[bitPos >> 3] >> (7 - (bitPos & 7))) & 1;
                code = (code << 1) | bit;
                bitPos++;
            }

            if (code == Eod)
            {
                break;
            }

            if (code == Clear)
            {
                ResetTable();
                width = 9;
                nextCode = 258;
                previous = -1;
                continue;
            }

            if (previous < 0)
            {
                if (code < 0 || code >= nextCode)
                {
                    throw new DocumentParseException("Invalid LZW code.", -1);
                }

                WriteEntry(code);
            }
            else
            {
                if (previous >= nextCode)
                {
                    throw new DocumentParseException("Invalid LZW code.", -1);
                }

                if (code < nextCode)
                {
                    if (code < 0)
                    {
                        throw new DocumentParseException("Invalid LZW code.", -1);
                    }

                    AddEntry(previous, first[code]);
                }
                else if (code == nextCode)
                {
                    // KwKwK: the only code the decoder is allowed to see before adding it.
                    AddEntry(previous, first[previous]);
                }
                else
                {
                    // A code beyond the next slot cannot be reconstructed; reject it.
                    throw new DocumentParseException("Invalid LZW code.", -1);
                }

                nextCode++;
                WriteEntry(code);

                if (nextCode + early == (1 << width) && width < 12)
                {
                    width++;
                }
            }

            if (output.Length > maxOutput)
            {
                throw new DocumentParseException("Decoded stream exceeds the maximum allowed size.", -1);
            }

            previous = code;
        }

        return output.ToArray();
    }
}

internal sealed class LzwStreamFilter : IStreamFilter
{
    public string Name => "LZWDecode";

    public byte[] Decode(byte[] data, DictionaryObject? parms, long maxOutput)
        => StreamPredictor.Apply(
            LzwFilter.Decode(data, parms is not null ? StreamPredictor.ParmInt(parms, "EarlyChange", 1) : 1, maxOutput),
            parms);
}
