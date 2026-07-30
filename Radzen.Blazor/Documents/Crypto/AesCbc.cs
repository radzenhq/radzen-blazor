using System;
using System.IO;

namespace Radzen.Documents.Crypto;

// FIPS 197 table-driven AES: the SBox/InvSBox lookups below are data-dependent and therefore
// not constant-time, so this implementation leaks key material to a cache-timing attacker. That
// is outside the threat model - it only decrypts PDF content the attacker already possesses.
internal static class AesCbc
{
    private static readonly byte[] SBox =
    [
        0x63, 0x7c, 0x77, 0x7b, 0xf2, 0x6b, 0x6f, 0xc5, 0x30, 0x01, 0x67, 0x2b, 0xfe, 0xd7, 0xab, 0x76,
        0xca, 0x82, 0xc9, 0x7d, 0xfa, 0x59, 0x47, 0xf0, 0xad, 0xd4, 0xa2, 0xaf, 0x9c, 0xa4, 0x72, 0xc0,
        0xb7, 0xfd, 0x93, 0x26, 0x36, 0x3f, 0xf7, 0xcc, 0x34, 0xa5, 0xe5, 0xf1, 0x71, 0xd8, 0x31, 0x15,
        0x04, 0xc7, 0x23, 0xc3, 0x18, 0x96, 0x05, 0x9a, 0x07, 0x12, 0x80, 0xe2, 0xeb, 0x27, 0xb2, 0x75,
        0x09, 0x83, 0x2c, 0x1a, 0x1b, 0x6e, 0x5a, 0xa0, 0x52, 0x3b, 0xd6, 0xb3, 0x29, 0xe3, 0x2f, 0x84,
        0x53, 0xd1, 0x00, 0xed, 0x20, 0xfc, 0xb1, 0x5b, 0x6a, 0xcb, 0xbe, 0x39, 0x4a, 0x4c, 0x58, 0xcf,
        0xd0, 0xef, 0xaa, 0xfb, 0x43, 0x4d, 0x33, 0x85, 0x45, 0xf9, 0x02, 0x7f, 0x50, 0x3c, 0x9f, 0xa8,
        0x51, 0xa3, 0x40, 0x8f, 0x92, 0x9d, 0x38, 0xf5, 0xbc, 0xb6, 0xda, 0x21, 0x10, 0xff, 0xf3, 0xd2,
        0xcd, 0x0c, 0x13, 0xec, 0x5f, 0x97, 0x44, 0x17, 0xc4, 0xa7, 0x7e, 0x3d, 0x64, 0x5d, 0x19, 0x73,
        0x60, 0x81, 0x4f, 0xdc, 0x22, 0x2a, 0x90, 0x88, 0x46, 0xee, 0xb8, 0x14, 0xde, 0x5e, 0x0b, 0xdb,
        0xe0, 0x32, 0x3a, 0x0a, 0x49, 0x06, 0x24, 0x5c, 0xc2, 0xd3, 0xac, 0x62, 0x91, 0x95, 0xe4, 0x79,
        0xe7, 0xc8, 0x37, 0x6d, 0x8d, 0xd5, 0x4e, 0xa9, 0x6c, 0x56, 0xf4, 0xea, 0x65, 0x7a, 0xae, 0x08,
        0xba, 0x78, 0x25, 0x2e, 0x1c, 0xa6, 0xb4, 0xc6, 0xe8, 0xdd, 0x74, 0x1f, 0x4b, 0xbd, 0x8b, 0x8a,
        0x70, 0x3e, 0xb5, 0x66, 0x48, 0x03, 0xf6, 0x0e, 0x61, 0x35, 0x57, 0xb9, 0x86, 0xc1, 0x1d, 0x9e,
        0xe1, 0xf8, 0x98, 0x11, 0x69, 0xd9, 0x8e, 0x94, 0x9b, 0x1e, 0x87, 0xe9, 0xce, 0x55, 0x28, 0xdf,
        0x8c, 0xa1, 0x89, 0x0d, 0xbf, 0xe6, 0x42, 0x68, 0x41, 0x99, 0x2d, 0x0f, 0xb0, 0x54, 0xbb, 0x16,
    ];

    private static readonly byte[] InvSBox = BuildInverse();

    private static readonly byte[] Rcon = [0x00, 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80, 0x1b, 0x36];

    public static byte[] Decrypt(byte[] key, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(data);
        if (data.Length < 16)
        {
            throw new InvalidDataException("AES data is shorter than the required 16-byte IV.");
        }

        var iv = data[..16];
        var cipher = data[16..];
        if (cipher.Length == 0)
        {
            throw new InvalidDataException("AES ciphertext after the IV is empty.");
        }

        var plain = DecryptCbcNoPadding(key, iv, cipher);
        return StripPadding(plain);
    }

    public static byte[] DecryptCbcNoPadding(byte[] key, byte[] iv, byte[] cipher)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(iv);
        ArgumentNullException.ThrowIfNull(cipher);
        RequireIv(iv);
        if (cipher.Length % 16 != 0)
        {
            throw new InvalidDataException("AES ciphertext length must be a whole number of 16-byte blocks.");
        }

        var roundKeys = ExpandKey(key, out var rounds);
        var whole = cipher.Length;
        var result = new byte[whole];
        var previous = (byte[])iv.Clone();
        var block = new byte[16];
        for (var offset = 0; offset < whole; offset += 16)
        {
            Array.Copy(cipher, offset, block, 0, 16);
            var decrypted = DecryptBlock(block, roundKeys, rounds);
            for (var i = 0; i < 16; i++)
            {
                result[offset + i] = (byte)(decrypted[i] ^ previous[i]);
            }

            Array.Copy(cipher, offset, previous, 0, 16);
        }

        return result;
    }

    public static byte[] EncryptCbcNoPadding(byte[] key, byte[] iv, byte[] plain)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(iv);
        ArgumentNullException.ThrowIfNull(plain);
        RequireIv(iv);
        if (plain.Length % 16 != 0)
        {
            throw new ArgumentException("AES plaintext length must be a whole number of 16-byte blocks.", nameof(plain));
        }

        var roundKeys = ExpandKey(key, out var rounds);
        var result = new byte[plain.Length];
        var previous = (byte[])iv.Clone();
        var block = new byte[16];
        for (var offset = 0; offset < plain.Length; offset += 16)
        {
            for (var i = 0; i < 16; i++)
            {
                block[i] = (byte)(plain[offset + i] ^ previous[i]);
            }

            var encrypted = EncryptBlock(block, roundKeys, rounds);
            Array.Copy(encrypted, 0, result, offset, 16);
            Array.Copy(encrypted, 0, previous, 0, 16);
        }

        return result;
    }

    private static void RequireIv(byte[] iv)
    {
        if (iv.Length != 16)
        {
            throw new ArgumentException("AES initialization vector must be exactly 16 bytes.", nameof(iv));
        }
    }

    private static byte[] StripPadding(byte[] plain)
    {
        if (plain.Length == 0)
        {
            throw new InvalidDataException("AES plaintext is empty.");
        }

        var pad = plain[^1];
        if (pad < 1 || pad > 16 || pad > plain.Length)
        {
            throw new InvalidDataException("Invalid PKCS#7 padding.");
        }

        for (var i = plain.Length - pad; i < plain.Length; i++)
        {
            if (plain[i] != pad)
            {
                throw new InvalidDataException("Invalid PKCS#7 padding.");
            }
        }

        return plain[..^pad];
    }

    private static byte[] EncryptBlock(byte[] input, byte[] roundKeys, int rounds)
    {
        var state = (byte[])input.Clone();
        AddRoundKey(state, roundKeys, 0);
        for (var round = 1; round < rounds; round++)
        {
            SubBytes(state);
            ShiftRows(state);
            MixColumns(state);
            AddRoundKey(state, roundKeys, round);
        }

        SubBytes(state);
        ShiftRows(state);
        AddRoundKey(state, roundKeys, rounds);
        return state;
    }

    private static byte[] DecryptBlock(byte[] input, byte[] roundKeys, int rounds)
    {
        var state = (byte[])input.Clone();
        AddRoundKey(state, roundKeys, rounds);
        for (var round = rounds - 1; round >= 1; round--)
        {
            InvShiftRows(state);
            InvSubBytes(state);
            AddRoundKey(state, roundKeys, round);
            InvMixColumns(state);
        }

        InvShiftRows(state);
        InvSubBytes(state);
        AddRoundKey(state, roundKeys, 0);
        return state;
    }

    private static void AddRoundKey(byte[] state, byte[] roundKeys, int round)
    {
        var offset = round * 16;
        for (var i = 0; i < 16; i++)
        {
            state[i] ^= roundKeys[offset + i];
        }
    }

    private static void SubBytes(byte[] state)
    {
        for (var i = 0; i < 16; i++)
        {
            state[i] = SBox[state[i]];
        }
    }

    private static void InvSubBytes(byte[] state)
    {
        for (var i = 0; i < 16; i++)
        {
            state[i] = InvSBox[state[i]];
        }
    }

    private static void ShiftRows(byte[] s)
    {
        Rotate(s, 1, left: true);
        Rotate(s, 2, left: true);
        Rotate(s, 3, left: true);
    }

    private static void InvShiftRows(byte[] s)
    {
        Rotate(s, 1, left: false);
        Rotate(s, 2, left: false);
        Rotate(s, 3, left: false);
    }

    private static void Rotate(byte[] s, int row, bool left)
    {
        var r = new byte[4];
        for (var c = 0; c < 4; c++)
        {
            r[c] = s[row + (4 * c)];
        }

        for (var c = 0; c < 4; c++)
        {
            var source = left ? (c + row) % 4 : ((c - row) % 4 + 4) % 4;
            s[row + (4 * c)] = r[source];
        }
    }

    private static void MixColumns(byte[] s)
    {
        for (var c = 0; c < 4; c++)
        {
            var i = 4 * c;
            var a0 = s[i];
            var a1 = s[i + 1];
            var a2 = s[i + 2];
            var a3 = s[i + 3];
            s[i] = (byte)(Mul(a0, 2) ^ Mul(a1, 3) ^ a2 ^ a3);
            s[i + 1] = (byte)(a0 ^ Mul(a1, 2) ^ Mul(a2, 3) ^ a3);
            s[i + 2] = (byte)(a0 ^ a1 ^ Mul(a2, 2) ^ Mul(a3, 3));
            s[i + 3] = (byte)(Mul(a0, 3) ^ a1 ^ a2 ^ Mul(a3, 2));
        }
    }

    private static void InvMixColumns(byte[] s)
    {
        for (var c = 0; c < 4; c++)
        {
            var i = 4 * c;
            var a0 = s[i];
            var a1 = s[i + 1];
            var a2 = s[i + 2];
            var a3 = s[i + 3];
            s[i] = (byte)(Mul(a0, 14) ^ Mul(a1, 11) ^ Mul(a2, 13) ^ Mul(a3, 9));
            s[i + 1] = (byte)(Mul(a0, 9) ^ Mul(a1, 14) ^ Mul(a2, 11) ^ Mul(a3, 13));
            s[i + 2] = (byte)(Mul(a0, 13) ^ Mul(a1, 9) ^ Mul(a2, 14) ^ Mul(a3, 11));
            s[i + 3] = (byte)(Mul(a0, 11) ^ Mul(a1, 13) ^ Mul(a2, 9) ^ Mul(a3, 14));
        }
    }

    private static byte Mul(byte value, int factor)
    {
        byte result = 0;
        byte a = value;
        var b = factor;
        while (b != 0)
        {
            if ((b & 1) != 0)
            {
                result ^= a;
            }

            var highBit = (byte)(a & 0x80);
            a <<= 1;
            if (highBit != 0)
            {
                a ^= 0x1b;
            }

            b >>= 1;
        }

        return result;
    }

    private static byte[] ExpandKey(byte[] key, out int rounds)
    {
        // FIPS-197 5.2: AES keys are 128/192/256-bit.
        if (key.Length is not (16 or 24 or 32))
        {
            throw new InvalidDataException("AES key length must be 16, 24, or 32 bytes.");
        }

        var nk = key.Length / 4;
        rounds = nk + 6;
        var totalWords = 4 * (rounds + 1);
        var words = new byte[totalWords * 4];
        Array.Copy(key, words, key.Length);

        var temp = new byte[4];
        for (var i = nk; i < totalWords; i++)
        {
            Array.Copy(words, (i - 1) * 4, temp, 0, 4);
            if (i % nk == 0)
            {
                (temp[0], temp[1], temp[2], temp[3]) = (temp[1], temp[2], temp[3], temp[0]);
                for (var k = 0; k < 4; k++)
                {
                    temp[k] = SBox[temp[k]];
                }

                temp[0] ^= Rcon[i / nk];
            }
            else if (nk > 6 && i % nk == 4)
            {
                for (var k = 0; k < 4; k++)
                {
                    temp[k] = SBox[temp[k]];
                }
            }

            for (var k = 0; k < 4; k++)
            {
                words[(i * 4) + k] = (byte)(words[((i - nk) * 4) + k] ^ temp[k]);
            }
        }

        return words;
    }

    private static byte[] BuildInverse()
    {
        var inverse = new byte[256];
        for (var i = 0; i < 256; i++)
        {
            inverse[SBox[i]] = (byte)i;
        }

        return inverse;
    }
}
