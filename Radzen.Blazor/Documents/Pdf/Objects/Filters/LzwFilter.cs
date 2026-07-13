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
    public static byte[] Decode(byte[] data, int early, long maxOutput)
    {
        ArgumentNullException.ThrowIfNull(data);

        var output = new List<byte>();
        if (data.Length == 0)
        {
            return [.. output];
        }

        var table = new List<byte[]>();
        void ResetTable()
        {
            table.Clear();
            for (int i = 0; i < 256; i++)
            {
                table.Add([(byte)i]);
            }

            table.Add([]);
            table.Add([]);
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

            byte[] entry;
            if (previous < 0)
            {
                if (code < 0 || code >= table.Count)
                {
                    throw new DocumentParseException("Invalid LZW code.", -1);
                }

                entry = table[code];
            }
            else
            {
                if (previous >= table.Count)
                {
                    throw new DocumentParseException("Invalid LZW code.", -1);
                }

                byte[] prevEntry = table[previous];
                if (code < nextCode)
                {
                    if (code < 0 || code >= table.Count)
                    {
                        throw new DocumentParseException("Invalid LZW code.", -1);
                    }

                    entry = table[code];
                }
                else if (code == nextCode)
                {
                    // KwKwK: the only code the decoder is allowed to see before adding it.
                    entry = new byte[prevEntry.Length + 1];
                    Array.Copy(prevEntry, entry, prevEntry.Length);
                    entry[prevEntry.Length] = prevEntry[0];
                }
                else
                {
                    // A code beyond the next slot cannot be reconstructed; reject it.
                    throw new DocumentParseException("Invalid LZW code.", -1);
                }

                var newEntry = new byte[prevEntry.Length + 1];
                Array.Copy(prevEntry, newEntry, prevEntry.Length);
                newEntry[prevEntry.Length] = entry[0];
                table.Add(newEntry);
                nextCode++;

                if (nextCode + early == (1 << width) && width < 12)
                {
                    width++;
                }
            }

            output.AddRange(entry);
            if (output.Count > maxOutput)
            {
                throw new DocumentParseException("Decoded stream exceeds the maximum allowed size.", -1);
            }

            previous = code;
        }

        return [.. output];
    }
}
