using System;

namespace Radzen.Documents.Crypto;

/// <summary>
/// Hand-rolled SHA-2 (FIPS 180-4): SHA-256, SHA-384 and SHA-512. The BCL
/// implementations throw <see cref="PlatformNotSupportedException"/> under Blazor
/// WebAssembly, so these managed variants are used instead.
/// </summary>
public static class Sha2
{
    internal static readonly uint[] K256 =
    [
        0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
        0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
        0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
        0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
        0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
        0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
        0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
        0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2,
    ];

    private static readonly ulong[] K512 =
    [
        0x428a2f98d728ae22, 0x7137449123ef65cd, 0xb5c0fbcfec4d3b2f, 0xe9b5dba58189dbbc,
        0x3956c25bf348b538, 0x59f111f1b605d019, 0x923f82a4af194f9b, 0xab1c5ed5da6d8118,
        0xd807aa98a3030242, 0x12835b0145706fbe, 0x243185be4ee4b28c, 0x550c7dc3d5ffb4e2,
        0x72be5d74f27b896f, 0x80deb1fe3b1696b1, 0x9bdc06a725c71235, 0xc19bf174cf692694,
        0xe49b69c19ef14ad2, 0xefbe4786384f25e3, 0x0fc19dc68b8cd5b5, 0x240ca1cc77ac9c65,
        0x2de92c6f592b0275, 0x4a7484aa6ea6e483, 0x5cb0a9dcbd41fbd4, 0x76f988da831153b5,
        0x983e5152ee66dfab, 0xa831c66d2db43210, 0xb00327c898fb213f, 0xbf597fc7beef0ee4,
        0xc6e00bf33da88fc2, 0xd5a79147930aa725, 0x06ca6351e003826f, 0x142929670a0e6e70,
        0x27b70a8546d22ffc, 0x2e1b21385c26c926, 0x4d2c6dfc5ac42aed, 0x53380d139d95b3df,
        0x650a73548baf63de, 0x766a0abb3c77b2a8, 0x81c2c92e47edaee6, 0x92722c851482353b,
        0xa2bfe8a14cf10364, 0xa81a664bbc423001, 0xc24b8b70d0f89791, 0xc76c51a30654be30,
        0xd192e819d6ef5218, 0xd69906245565a910, 0xf40e35855771202a, 0x106aa07032bbd1b8,
        0x19a4c116b8d2d0c8, 0x1e376c085141ab53, 0x2748774cdf8eeb99, 0x34b0bcb5e19b48a8,
        0x391c0cb3c5c95a63, 0x4ed8aa4ae3418acb, 0x5b9cca4f7763e373, 0x682e6ff3d6b2b8a3,
        0x748f82ee5defb2fc, 0x78a5636f43172f60, 0x84c87814a1f0ab72, 0x8cc702081a6439ec,
        0x90befffa23631e28, 0xa4506cebde82bde9, 0xbef9a3f7b2c67915, 0xc67178f2e372532b,
        0xca273eceea26619c, 0xd186b8c721c0c207, 0xeada7dd6cde0eb1e, 0xf57d4f7fee6ed178,
        0x06f067aa72176fba, 0x0a637dc5a2c898a6, 0x113f9804bef90dae, 0x1b710b35131c471b,
        0x28db77f523047d84, 0x32caab7b40c72493, 0x3c9ebe0a15c9bebc, 0x431d67c49c100d4c,
        0x4cc5d4becb3e42b6, 0x597f299cfc657e2a, 0x5fcb6fab3ad6faec, 0x6c44198c4a475817,
    ];

    /// <summary>
    /// Computes the 32-byte SHA-256 digest of <paramref name="data"/>.
    /// </summary>
    /// <param name="data">The bytes to hash.</param>
    /// <returns>The 32-byte digest.</returns>
    public static byte[] ComputeHash256(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        return ComputeHash256((ReadOnlySpan<byte>)data);
    }

    /// <summary>
    /// Computes the 32-byte SHA-256 digest of <paramref name="data"/>.
    /// </summary>
    /// <param name="data">The bytes to hash.</param>
    /// <returns>The 32-byte digest.</returns>
    public static byte[] ComputeHash256(ReadOnlySpan<byte> data)
    {
        var hasher = new Sha256Hasher();
        hasher.Append(data);
        return hasher.Finish();
    }

    /// <summary>
    /// Computes the 48-byte SHA-384 digest of <paramref name="data"/>.
    /// </summary>
    /// <param name="data">The bytes to hash.</param>
    /// <returns>The 48-byte digest.</returns>
    public static byte[] ComputeHash384(byte[] data)
        => Sha512Core(
            data,
            [
                0xcbbb9d5dc1059ed8, 0x629a292a367cd507, 0x9159015a3070dd17, 0x152fecd8f70e5939,
                0x67332667ffc00b31, 0x8eb44a8768581511, 0xdb0c2e0d64f98fa7, 0x47b5481dbefa4fa4,
            ],
            48);

    /// <summary>
    /// Computes the 64-byte SHA-512 digest of <paramref name="data"/>.
    /// </summary>
    /// <param name="data">The bytes to hash.</param>
    /// <returns>The 64-byte digest.</returns>
    public static byte[] ComputeHash512(byte[] data)
        => Sha512Core(
            data,
            [
                0x6a09e667f3bcc908, 0xbb67ae8584caa73b, 0x3c6ef372fe94f82b, 0xa54ff53a5f1d36f1,
                0x510e527fade682d1, 0x9b05688c2b3e6c1f, 0x1f83d9abfb41bd6b, 0x5be0cd19137e2179,
            ],
            64);

    /// <summary>
    /// Computes the SHA-256 digest as a lowercase hexadecimal string.
    /// </summary>
    public static string ComputeHashHex256(byte[] data) => ToHex(ComputeHash256(data));

    /// <summary>
    /// Computes the SHA-384 digest as a lowercase hexadecimal string.
    /// </summary>
    public static string ComputeHashHex384(byte[] data) => ToHex(ComputeHash384(data));

    /// <summary>
    /// Computes the SHA-512 digest as a lowercase hexadecimal string.
    /// </summary>
    public static string ComputeHashHex512(byte[] data) => ToHex(ComputeHash512(data));

    private static byte[] Sha512Core(byte[] data, ReadOnlySpan<ulong> init, int outputBytes)
    {
        Span<ulong> h = stackalloc ulong[8];
        init.CopyTo(h);

        var padded = Pad128(data);
        Span<ulong> w = stackalloc ulong[80];
        for (var offset = 0; offset < padded.Length; offset += 128)
        {
            for (var i = 0; i < 16; i++)
            {
                w[i] = ReadUInt64(padded, offset + (i * 8));
            }

            for (var i = 16; i < 80; i++)
            {
                var s0 = RotR64(w[i - 15], 1) ^ RotR64(w[i - 15], 8) ^ (w[i - 15] >> 7);
                var s1 = RotR64(w[i - 2], 19) ^ RotR64(w[i - 2], 61) ^ (w[i - 2] >> 6);
                w[i] = w[i - 16] + s0 + w[i - 7] + s1;
            }

            ulong a = h[0], b = h[1], c = h[2], d = h[3], e = h[4], f = h[5], g = h[6], hh = h[7];
            for (var i = 0; i < 80; i++)
            {
                var s1 = RotR64(e, 14) ^ RotR64(e, 18) ^ RotR64(e, 41);
                var ch = (e & f) ^ (~e & g);
                var t1 = hh + s1 + ch + K512[i] + w[i];
                var s0 = RotR64(a, 28) ^ RotR64(a, 34) ^ RotR64(a, 39);
                var maj = (a & b) ^ (a & c) ^ (b & c);
                var t2 = s0 + maj;
                hh = g;
                g = f;
                f = e;
                e = d + t1;
                d = c;
                c = b;
                b = a;
                a = t1 + t2;
            }

            h[0] += a;
            h[1] += b;
            h[2] += c;
            h[3] += d;
            h[4] += e;
            h[5] += f;
            h[6] += g;
            h[7] += hh;
        }

        var result = new byte[outputBytes];
        for (var i = 0; i < outputBytes / 8; i++)
        {
            WriteUInt64(result, i * 8, h[i]);
        }

        return result;
    }

    private static byte[] Pad128(byte[] data)
    {
        var bitLength = (ulong)data.Length * 8;
        var total = data.Length + 1;
        var padZeros = (112 - (total % 128) + 128) % 128;
        var padded = new byte[total + padZeros + 16];
        Array.Copy(data, padded, data.Length);
        padded[data.Length] = 0x80;
        for (var i = 0; i < 8; i++)
        {
            padded[padded.Length - 1 - i] = (byte)(bitLength >> (8 * i));
        }

        return padded;
    }

    internal static uint RotR32(uint value, int bits) => (value >> bits) | (value << (32 - bits));

    private static ulong RotR64(ulong value, int bits) => (value >> bits) | (value << (64 - bits));

    private static uint ReadUInt32(byte[] data, int offset)
        => ((uint)data[offset] << 24) | ((uint)data[offset + 1] << 16) | ((uint)data[offset + 2] << 8) | data[offset + 3];

    private static ulong ReadUInt64(byte[] data, int offset)
        => ((ulong)ReadUInt32(data, offset) << 32) | ReadUInt32(data, offset + 4);

    internal static void WriteUInt32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value >> 24);
        data[offset + 1] = (byte)(value >> 16);
        data[offset + 2] = (byte)(value >> 8);
        data[offset + 3] = (byte)value;
    }

    private static void WriteUInt64(byte[] data, int offset, ulong value)
    {
        WriteUInt32(data, offset, (uint)(value >> 32));
        WriteUInt32(data, offset + 4, (uint)value);
    }

    private static string ToHex(byte[] digest) => HexCodec.EncodeToString(digest, HexCase.Lower);
}

/// <summary>
/// Incremental SHA-256 (FIPS 180-4): hashes data supplied in arbitrary chunks without
/// concatenating it first. Produces the same digest as <see cref="Sha2.ComputeHash256(byte[])"/>
/// for the same overall byte sequence. Not thread safe; single use - the instance is
/// finalized by <see cref="Finish"/>.
/// </summary>
public sealed class Sha256Hasher
{
    private readonly uint[] state =
    [
        0x6a09e667, 0xbb67ae85, 0x3c6ef372, 0xa54ff53a, 0x510e527f, 0x9b05688c, 0x1f83d9ab, 0x5be0cd19,
    ];

    private readonly byte[] block = new byte[64];
    private int blockLength;
    private ulong length;

    /// <summary>
    /// Appends <paramref name="data"/> to the running digest.
    /// </summary>
    /// <param name="data">The bytes to append.</param>
    public void Append(ReadOnlySpan<byte> data)
    {
        length += (ulong)data.Length;
        AppendCore(data);
    }

    /// <summary>
    /// Appends a single byte to the running digest.
    /// </summary>
    /// <param name="value">The byte to append.</param>
    public void Append(byte value)
    {
        length++;
        block[blockLength++] = value;
        if (blockLength == 64)
        {
            Transform(block);
            blockLength = 0;
        }
    }

    /// <summary>
    /// Applies the FIPS 180-4 padding and returns the 32-byte digest.
    /// </summary>
    /// <returns>The 32-byte digest.</returns>
    public byte[] Finish()
    {
        var bitLength = length * 8;
        Span<byte> tail = stackalloc byte[72];
        tail.Clear();
        tail[0] = 0x80;
        var padZeros = (56 - ((blockLength + 1) % 64) + 64) % 64;
        var tailLength = 1 + padZeros + 8;
        for (var i = 0; i < 8; i++)
        {
            tail[tailLength - 1 - i] = (byte)(bitLength >> (8 * i));
        }

        AppendCore(tail[..tailLength]);

        var result = new byte[32];
        for (var i = 0; i < 8; i++)
        {
            Sha2.WriteUInt32(result, i * 4, state[i]);
        }

        return result;
    }

    private void AppendCore(ReadOnlySpan<byte> data)
    {
        if (blockLength > 0)
        {
            var take = Math.Min(64 - blockLength, data.Length);
            data[..take].CopyTo(block.AsSpan(blockLength));
            blockLength += take;
            data = data[take..];
            if (blockLength < 64)
            {
                return;
            }

            Transform(block);
            blockLength = 0;
        }

        while (data.Length >= 64)
        {
            Transform(data[..64]);
            data = data[64..];
        }

        data.CopyTo(block);
        blockLength = data.Length;
    }

    private void Transform(ReadOnlySpan<byte> chunk)
    {
        Span<uint> w = stackalloc uint[64];
        for (var i = 0; i < 16; i++)
        {
            var offset = i * 4;
            w[i] = ((uint)chunk[offset] << 24) | ((uint)chunk[offset + 1] << 16)
                | ((uint)chunk[offset + 2] << 8) | chunk[offset + 3];
        }

        for (var i = 16; i < 64; i++)
        {
            var s0 = Sha2.RotR32(w[i - 15], 7) ^ Sha2.RotR32(w[i - 15], 18) ^ (w[i - 15] >> 3);
            var s1 = Sha2.RotR32(w[i - 2], 17) ^ Sha2.RotR32(w[i - 2], 19) ^ (w[i - 2] >> 10);
            w[i] = w[i - 16] + s0 + w[i - 7] + s1;
        }

        uint a = state[0], b = state[1], c = state[2], d = state[3];
        uint e = state[4], f = state[5], g = state[6], hh = state[7];
        for (var i = 0; i < 64; i++)
        {
            var s1 = Sha2.RotR32(e, 6) ^ Sha2.RotR32(e, 11) ^ Sha2.RotR32(e, 25);
            var ch = (e & f) ^ (~e & g);
            var t1 = hh + s1 + ch + Sha2.K256[i] + w[i];
            var s0 = Sha2.RotR32(a, 2) ^ Sha2.RotR32(a, 13) ^ Sha2.RotR32(a, 22);
            var maj = (a & b) ^ (a & c) ^ (b & c);
            var t2 = s0 + maj;
            hh = g;
            g = f;
            f = e;
            e = d + t1;
            d = c;
            c = b;
            b = a;
            a = t1 + t2;
        }

        state[0] += a;
        state[1] += b;
        state[2] += c;
        state[3] += d;
        state[4] += e;
        state[5] += f;
        state[6] += g;
        state[7] += hh;
    }
}
