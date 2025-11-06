using System;
using System.Linq;

namespace Radzen;

/// <summary>
/// MD5 hash calculator.
/// </summary>
public class MD5
{
    /*
     * Round shift values
     */
    private static int[] s = new int[64] {
        7, 12, 17, 22,  7, 12, 17, 22,  7, 12, 17, 22,  7, 12, 17, 22,
        5,  9, 14, 20,  5,  9, 14, 20,  5,  9, 14, 20,  5,  9, 14, 20,
        4, 11, 16, 23,  4, 11, 16, 23,  4, 11, 16, 23,  4, 11, 16, 23,
        6, 10, 15, 21,  6, 10, 15, 21,  6, 10, 15, 21,  6, 10, 15, 21
    };

    /*
     * Constant K Values
     */
    private static uint[] k = new uint[64] {
        0xd76aa478, 0xe8c7b756, 0x242070db, 0xc1bdceee,
        0xf57c0faf, 0x4787c62a, 0xa8304613, 0xfd469501,
        0x698098d8, 0x8b44f7af, 0xffff5bb1, 0x895cd7be,
        0x6b901122, 0xfd987193, 0xa679438e, 0x49b40821,
        0xf61e2562, 0xc040b340, 0x265e5a51, 0xe9b6c7aa,
        0xd62f105d, 0x02441453, 0xd8a1e681, 0xe7d3fbc8,
        0x21e1cde6, 0xc33707d6, 0xf4d50d87, 0x455a14ed,
        0xa9e3e905, 0xfcefa3f8, 0x676f02d9, 0x8d2a4c8a,
        0xfffa3942, 0x8771f681, 0x6d9d6122, 0xfde5380c,
        0xa4beea44, 0x4bdecfa9, 0xf6bb4b60, 0xbebfbc70,
        0x289b7ec6, 0xeaa127fa, 0xd4ef3085, 0x04881d05,
        0xd9d4d039, 0xe6db99e5, 0x1fa27cf8, 0xc4ac5665,
        0xf4292244, 0x432aff97, 0xab9423a7, 0xfc93a039,
        0x655b59c3, 0x8f0ccc92, 0xffeff47d, 0x85845dd1,
        0x6fa87e4f, 0xfe2ce6e0, 0xa3014314, 0x4e0811a1,
        0xf7537e82, 0xbd3af235, 0x2ad7d2bb, 0xeb86d391
    };

    /// <summary>
    /// Performs left rotation of bits.
    /// </summary>
    /// <param name="x">The value.</param>
    /// <param name="c">The rotation count.</param>
    /// <returns>The rotated value.</returns>
    public static uint leftRotate(uint x, int c)
    {
        return (x << c) | (x >> (32 - c));
    }

    // assumes whole bytes as input
    /// <summary>
    /// Calculates the MD5 hash.
    /// </summary>
    /// <param name="input">The input bytes.</param>
    /// <returns>The MD5 hash as a string.</returns>
    public static string Calculate(byte[] input)
    {
        uint a0 = 0x67452301;   // A
        uint b0 = 0xefcdab89;   // B
        uint c0 = 0x98badcfe;   // C
        uint d0 = 0x10325476;   // D

        var addLength = (56 - ((input.Length + 1) % 64)) % 64; // calculate the new length with padding
        var processedInput = new byte[input.Length + 1 + addLength + 8];
        Array.Copy(input, processedInput, input.Length);
        processedInput[input.Length] = 0x80; // add 1

        byte[] length = BitConverter.GetBytes(input.Length * 8); // bit converter returns little-endian
        Array.Copy(length, 0, processedInput, processedInput.Length - 8, 4); // add length in bits

        for (int i = 0; i < processedInput.Length / 64; ++i)
        {
            // copy the input to M
            uint[] M = new uint[16];
            for (int j = 0; j < 16; ++j)
                M[j] = BitConverter.ToUInt32(processedInput, (i * 64) + (j * 4));

            // initialize round variables
            uint A = a0, B = b0, C = c0, D = d0, F = 0, g = 0;

            // primary loop
            for (uint ki = 0; ki < 64; ++ki)
            {
                if (ki <= 15)
                {
                    F = (B & C) | (~B & D);
                    g = ki;
                }
                else if (ki >= 16 && ki <= 31)
                {
                    F = (D & B) | (~D & C);
                    g = ((5 * ki) + 1) % 16;
                }
                else if (ki >= 32 && ki <= 47)
                {
                    F = B ^ C ^ D;
                    g = ((3 * ki) + 5) % 16;
                }
                else if (ki >= 48)
                {
                    F = C ^ (B | ~D);
                    g = (7 * ki) % 16;
                }

                var dtemp = D;
                D = C;
                C = B;
                B = B + leftRotate((A + F + k[ki] + M[g]), s[ki]);
                A = dtemp;
            }

            a0 += A;
            b0 += B;
            c0 += C;
            d0 += D;
        }

        return GetByteString(a0) + GetByteString(b0) + GetByteString(c0) + GetByteString(d0);
    }

    private static string GetByteString(uint x)
    {
        return String.Join("", BitConverter.GetBytes(x).Select(y => y.ToString("x2")));
    }
}

