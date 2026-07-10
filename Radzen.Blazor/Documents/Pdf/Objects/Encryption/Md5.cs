using System;

namespace Radzen.Documents.Pdf.Objects.Encryption;

/// <summary>
/// Hand-rolled MD5 (RFC 1321). The BCL <c>System.Security.Cryptography.MD5</c>
/// throws on browser-wasm, so the standard security handler needs its own.
/// </summary>
internal static class Md5
{
    private static readonly uint[] K =
    [
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
        0xf7537e82, 0xbd3af235, 0x2ad7d2bb, 0xeb86d391,
    ];

    private static readonly int[] S =
    [
        7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22, 7, 12, 17, 22,
        5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20, 5, 9, 14, 20,
        4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23, 4, 11, 16, 23,
        6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21, 6, 10, 15, 21,
    ];

    public static byte[] Hash(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var padded = Pad(data);
        uint a0 = 0x67452301, b0 = 0xefcdab89, c0 = 0x98badcfe, d0 = 0x10325476;
        var m = new uint[16];

        for (var block = 0; block < padded.Length; block += 64)
        {
            for (var i = 0; i < 16; i++)
            {
                var j = block + (i * 4);
                m[i] = (uint)(padded[j] | (padded[j + 1] << 8) | (padded[j + 2] << 16) | (padded[j + 3] << 24));
            }

            uint a = a0, b = b0, c = c0, d = d0;
            for (var i = 0; i < 64; i++)
            {
                uint f;
                int g;
                if (i < 16)
                {
                    f = (b & c) | (~b & d);
                    g = i;
                }
                else if (i < 32)
                {
                    f = (d & b) | (~d & c);
                    g = ((5 * i) + 1) % 16;
                }
                else if (i < 48)
                {
                    f = b ^ c ^ d;
                    g = ((3 * i) + 5) % 16;
                }
                else
                {
                    f = c ^ (b | ~d);
                    g = (7 * i) % 16;
                }

                f = f + a + K[i] + m[g];
                a = d;
                d = c;
                c = b;
                b += RotateLeft(f, S[i]);
            }

            a0 += a;
            b0 += b;
            c0 += c;
            d0 += d;
        }

        var result = new byte[16];
        WriteLittleEndian(result, 0, a0);
        WriteLittleEndian(result, 4, b0);
        WriteLittleEndian(result, 8, c0);
        WriteLittleEndian(result, 12, d0);
        return result;
    }

    private static byte[] Pad(byte[] data)
    {
        var bitLength = (ulong)data.Length * 8;
        var totalLength = data.Length + 1;
        while (totalLength % 64 != 56)
        {
            totalLength++;
        }

        var padded = new byte[totalLength + 8];
        Array.Copy(data, padded, data.Length);
        padded[data.Length] = 0x80;
        for (var i = 0; i < 8; i++)
        {
            padded[totalLength + i] = (byte)(bitLength >> (8 * i));
        }

        return padded;
    }

    private static uint RotateLeft(uint value, int count) => (value << count) | (value >> (32 - count));

    private static void WriteLittleEndian(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)value;
        buffer[offset + 1] = (byte)(value >> 8);
        buffer[offset + 2] = (byte)(value >> 16);
        buffer[offset + 3] = (byte)(value >> 24);
    }
}
