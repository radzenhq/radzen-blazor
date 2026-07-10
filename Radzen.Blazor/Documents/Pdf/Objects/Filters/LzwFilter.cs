using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Objects.Filters
{
    /// <summary>
    /// Implements the PDF <c>LZWDecode</c> filter with MSB-first bit packing, variable
    /// 9-12 bit codes, and configurable early change.
    /// </summary>
    public static class LzwFilter
    {
        const int Clear = 256;
        const int Eod = 257;

        /// <summary>
        /// Decodes an LZW stream.
        /// </summary>
        /// <param name="data">The encoded input.</param>
        /// <param name="early">The early-change value (1 to advance the code width one code early, 0 otherwise).</param>
        /// <returns>The decoded bytes.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="data"/> is <see langword="null"/>.</exception>
        public static byte[] Decode(byte[] data, int early)
        {
            ArgumentNullException.ThrowIfNull(data);

            var output = new List<byte>();
            if (data.Length == 0)
            {
                return output.ToArray();
            }

            var table = new List<byte[]>();
            void ResetTable()
            {
                table.Clear();
                for (int i = 0; i < 256; i++)
                {
                    table.Add(new[] { (byte)i });
                }

                table.Add(Array.Empty<byte>());
                table.Add(Array.Empty<byte>());
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
                    entry = table[code];
                }
                else
                {
                    byte[] prevEntry = table[previous];
                    if (code < nextCode)
                    {
                        entry = table[code];
                    }
                    else
                    {
                        entry = new byte[prevEntry.Length + 1];
                        Array.Copy(prevEntry, entry, prevEntry.Length);
                        entry[prevEntry.Length] = prevEntry[0];
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
                previous = code;
            }

            return output.ToArray();
        }
    }
}
