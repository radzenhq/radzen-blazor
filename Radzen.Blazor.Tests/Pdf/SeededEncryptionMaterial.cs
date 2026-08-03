#nullable enable
using System;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Crypto;

namespace Radzen.Blazor.Pdf.Tests;

internal sealed class SeededEncryptionMaterial : IEncryptionMaterial
{
    private readonly byte[] seed;

    public SeededEncryptionMaterial(byte[] seed)
    {
        ArgumentNullException.ThrowIfNull(seed);
        if (seed.Length == 0)
        {
            throw new ArgumentException("Seed must be non-empty.", nameof(seed));
        }

        this.seed = (byte[])seed.Clone();
    }

    public byte[] GetBytes(int index, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var result = new byte[count];
        var written = 0;
        var block = 0;
        while (written < count)
        {
            var prefix = new byte[seed.Length + 8];
            Array.Copy(seed, prefix, seed.Length);
            WriteInt(prefix, seed.Length, index);
            WriteInt(prefix, seed.Length + 4, block);
            var hash = Sha2.ComputeHash256(prefix);
            var take = Math.Min(hash.Length, count - written);
            Array.Copy(hash, 0, result, written, take);
            written += take;
            block++;
        }

        return result;
    }

    private static void WriteInt(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }
}
