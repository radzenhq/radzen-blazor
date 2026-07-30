#nullable enable
using System;
using System.Security.Cryptography;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 algorithms 2/3/5 (R4 handler), ISO 32000-2 algorithm 2.B (R6)
internal static class CryptoFixtureSupport
{
    // ISO 32000-1 standard password padding
    public static readonly byte[] Pad32 =
    [
        0x28, 0xBF, 0x4E, 0x5E, 0x4E, 0x75, 0x8A, 0x41, 0x64, 0x00, 0x4E, 0x56, 0xFF, 0xFA, 0x01, 0x08,
        0x2E, 0x2E, 0x00, 0xB6, 0xD0, 0x68, 0x3E, 0x80, 0x2F, 0x0C, 0xA9, 0xFE, 0x64, 0x53, 0x69, 0x7A,
    ];

    private static readonly byte[] AesSalt = [0x73, 0x41, 0x6C, 0x54];

    public static byte[] Rc4(byte[] key, byte[] data)
    {
        var s = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            s[i] = (byte)i;
        }

        var j = 0;
        for (var i = 0; i < 256; i++)
        {
            j = (j + s[i] + key[i % key.Length]) & 0xFF;
            (s[i], s[j]) = (s[j], s[i]);
        }

        var result = new byte[data.Length];
        int x = 0, y = 0;
        for (var k = 0; k < data.Length; k++)
        {
            x = (x + 1) & 0xFF;
            y = (y + s[x]) & 0xFF;
            (s[x], s[y]) = (s[y], s[x]);
            result[k] = (byte)(data[k] ^ s[(s[x] + s[y]) & 0xFF]);
        }

        return result;
    }

    private static byte[] Xor(byte[] key, int value)
    {
        var result = new byte[key.Length];
        for (var i = 0; i < key.Length; i++)
        {
            result[i] = (byte)(key[i] ^ value);
        }

        return result;
    }

    private static byte[] Concat(params byte[][] parts)
    {
        var length = 0;
        foreach (var part in parts)
        {
            length += part.Length;
        }

        var result = new byte[length];
        var pos = 0;
        foreach (var part in parts)
        {
            Array.Copy(part, 0, result, pos, part.Length);
            pos += part.Length;
        }

        return result;
    }

    // ISO 32000-1 Algorithm 3: empty user and owner passwords, R >= 3, 128-bit key
    public static byte[] R4OwnerEntry()
    {
        var hash = System.Security.Cryptography.MD5.HashData(Pad32);
        for (var i = 0; i < 50; i++)
        {
            hash = System.Security.Cryptography.MD5.HashData(hash[..16]);
        }

        var key = hash[..16];
        var value = Rc4(key, Pad32);
        for (var i = 1; i <= 19; i++)
        {
            value = Rc4(Xor(key, i), value);
        }

        return value;
    }

    // ISO 32000-1 Algorithm 2: empty user password, R4, 128-bit key, metadata encrypted
    public static byte[] R4FileKey(byte[] ownerEntry, int permissions, byte[] documentId)
    {
        var p = new byte[]
        {
            (byte)permissions,
            (byte)(permissions >> 8),
            (byte)(permissions >> 16),
            (byte)(permissions >> 24),
        };
        var hash = System.Security.Cryptography.MD5.HashData(Concat(Pad32, ownerEntry, p, documentId));
        for (var i = 0; i < 50; i++)
        {
            hash = System.Security.Cryptography.MD5.HashData(hash[..16]);
        }

        return hash[..16];
    }

    // ISO 32000-1 Algorithm 5: R >= 3, 16 significant bytes + 16 bytes padding
    public static byte[] R4UserEntry(byte[] fileKey, byte[] documentId)
    {
        var value = Rc4(fileKey, System.Security.Cryptography.MD5.HashData(Concat(Pad32, documentId)));
        for (var i = 1; i <= 19; i++)
        {
            value = Rc4(Xor(fileKey, i), value);
        }

        return Concat(value, new byte[16]);
    }

    // ISO 32000-1 Algorithm 1: object key for the AESV2 crypt filter
    public static byte[] AesV2ObjectKey(byte[] fileKey, int objectNumber, int generation)
    {
        var extra = new byte[]
        {
            (byte)objectNumber,
            (byte)(objectNumber >> 8),
            (byte)(objectNumber >> 16),
            (byte)generation,
            (byte)(generation >> 8),
        };
        return System.Security.Cryptography.MD5.HashData(Concat(fileKey, extra, AesSalt))[..16];
    }

    public static byte[] AesEncrypt(byte[] key, byte[] iv, byte[] plain)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.IV = iv;
        using var encryptor = aes.CreateEncryptor();
        var cipher = encryptor.TransformFinalBlock(plain, 0, plain.Length);
        return Concat(iv, cipher);
    }

    public static byte[] AesCbcNoPadding(byte[] key, byte[] iv, byte[] plain)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;
        aes.IV = iv;
        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(plain, 0, plain.Length);
    }

    // ISO 32000-2 algorithm 2.B hash
    public static byte[] Hash2B(byte[] password, byte[] salt, byte[] userData)
    {
        var k = SHA256.HashData(Concat(password, salt, userData));
        var round = 0;
        while (true)
        {
            var block = Concat(password, k, userData);
            var k1 = new byte[block.Length * 64];
            for (var i = 0; i < 64; i++)
            {
                Array.Copy(block, 0, k1, i * block.Length, block.Length);
            }

            var e = AesCbcNoPadding(k[..16], k[16..32], k1);
            var mod = 0;
            for (var i = 0; i < 16; i++)
            {
                mod = (mod + e[i]) % 3;
            }

            k = mod switch
            {
                0 => SHA256.HashData(e),
                1 => SHA384.HashData(e),
                _ => SHA512.HashData(e),
            };

            round++;
            if (round >= 64 && e[^1] <= round - 32)
            {
                break;
            }
        }

        return k[..32];
    }

    public static string Hex(byte[] bytes) => "<" + Convert.ToHexString(bytes) + ">";

    public static byte[] FixedBytes(int length, int seed)
    {
        var result = new byte[length];
        for (var i = 0; i < length; i++)
        {
            result[i] = (byte)((i * 31 + seed) & 0xFF);
        }

        return result;
    }
}
