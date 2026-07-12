using System;

namespace Radzen.Documents.Crypto;

/// <summary>
/// Hand-rolled SHA-1 (FIPS 180-4). The BCL implementation is unavailable under
/// Blazor WebAssembly, so this managed variant is used instead. In this library
/// SHA-1 serves only as the PAdES <c>/VRI</c> index key (a lookup digest, matching
/// the Adobe/ETSI convention), never as a security primitive.
/// </summary>
public static class Sha1
{
    /// <summary>
    /// Computes the 20-byte SHA-1 digest of <paramref name="data"/>.
    /// </summary>
    /// <param name="data">The bytes to hash.</param>
    /// <returns>The 20-byte digest.</returns>
    public static byte[] Hash(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        uint h0 = 0x67452301, h1 = 0xEFCDAB89, h2 = 0x98BADCFE, h3 = 0x10325476, h4 = 0xC3D2E1F0;

        var padded = Pad(data);
        Span<uint> w = stackalloc uint[80];
        for (var offset = 0; offset < padded.Length; offset += 64)
        {
            for (var i = 0; i < 16; i++)
            {
                w[i] = ((uint)padded[offset + i * 4] << 24)
                    | ((uint)padded[offset + i * 4 + 1] << 16)
                    | ((uint)padded[offset + i * 4 + 2] << 8)
                    | padded[offset + i * 4 + 3];
            }

            for (var i = 16; i < 80; i++)
            {
                w[i] = RotL(w[i - 3] ^ w[i - 8] ^ w[i - 14] ^ w[i - 16], 1);
            }

            uint a = h0, b = h1, c = h2, d = h3, e = h4;
            for (var i = 0; i < 80; i++)
            {
                uint f, k;
                if (i < 20)
                {
                    f = (b & c) | (~b & d);
                    k = 0x5A827999;
                }
                else if (i < 40)
                {
                    f = b ^ c ^ d;
                    k = 0x6ED9EBA1;
                }
                else if (i < 60)
                {
                    f = (b & c) | (b & d) | (c & d);
                    k = 0x8F1BBCDC;
                }
                else
                {
                    f = b ^ c ^ d;
                    k = 0xCA62C1D6;
                }

                var temp = RotL(a, 5) + f + e + k + w[i];
                e = d;
                d = c;
                c = RotL(b, 30);
                b = a;
                a = temp;
            }

            h0 += a;
            h1 += b;
            h2 += c;
            h3 += d;
            h4 += e;
        }

        var result = new byte[20];
        Write(result, 0, h0);
        Write(result, 4, h1);
        Write(result, 8, h2);
        Write(result, 12, h3);
        Write(result, 16, h4);
        return result;
    }

    /// <summary>
    /// Computes the SHA-1 digest of <paramref name="data"/> and returns it as an
    /// uppercase base-16 string (the form PAdES uses for <c>/VRI</c> keys).
    /// </summary>
    /// <param name="data">The bytes to hash.</param>
    /// <returns>The 40-character uppercase hexadecimal digest.</returns>
    public static string HexUpper(byte[] data)
    {
        var digest = Hash(data);
        const string hex = "0123456789ABCDEF";
        var chars = new char[digest.Length * 2];
        for (var i = 0; i < digest.Length; i++)
        {
            chars[i * 2] = hex[digest[i] >> 4];
            chars[i * 2 + 1] = hex[digest[i] & 0xF];
        }

        return new string(chars);
    }

    private static byte[] Pad(byte[] data)
    {
        var bitLength = (ulong)data.Length * 8;
        var total = data.Length + 1;
        var padZeros = (56 - (total % 64) + 64) % 64;
        var padded = new byte[total + padZeros + 8];
        Array.Copy(data, padded, data.Length);
        padded[data.Length] = 0x80;
        for (var i = 0; i < 8; i++)
        {
            padded[padded.Length - 1 - i] = (byte)(bitLength >> (8 * i));
        }

        return padded;
    }

    private static uint RotL(uint value, int bits) => (value << bits) | (value >> (32 - bits));

    private static void Write(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }
}
