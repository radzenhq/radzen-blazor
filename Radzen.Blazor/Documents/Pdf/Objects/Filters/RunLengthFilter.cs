using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Objects.Filters
{
    /// <summary>
    /// Implements the PDF <c>RunLengthDecode</c> filter.
    /// </summary>
    public static class RunLengthFilter
    {
        const int Eod = 128;

        /// <summary>
        /// Decodes run-length data. A length byte 0-127 copies the next length+1 bytes
        /// literally, 129-255 repeats the next byte 257-length times, and 128 marks end of data.
        /// </summary>
        /// <param name="data">The encoded input.</param>
        /// <returns>The decoded bytes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
        public static byte[] Decode(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            var output = new List<byte>();
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
                        output.Add(data[i++]);
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
                        output.Add(value);
                    }
                }
            }

            return output.ToArray();
        }

        /// <summary>
        /// Encodes bytes using run-length compression terminated by the end-of-data marker.
        /// </summary>
        /// <param name="data">The raw input.</param>
        /// <returns>The encoded bytes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
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
            return output.ToArray();
        }
    }
}
