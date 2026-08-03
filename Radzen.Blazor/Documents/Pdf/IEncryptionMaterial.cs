using System;
using System.Collections.Generic;
using Radzen.Documents.Pdf.Crypto;

namespace Radzen.Documents.Pdf;

internal interface IEncryptionMaterial
{
    byte[] GetBytes(int index, int count);
}

internal sealed class RandomEncryptionMaterial : IEncryptionMaterial
{
    public static readonly RandomEncryptionMaterial Instance = new();

    public byte[] GetBytes(int index, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        var result = new byte[count];
#pragma warning disable RS0030
        System.Security.Cryptography.RandomNumberGenerator.Fill(result);
#pragma warning restore RS0030
        return result;
    }
}

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

internal sealed class MaterialSequence(IEncryptionMaterial material)
{
    private int index;

    public int Position
    {
        get => index;
        set => index = value;
    }

    public byte[] Next(int count) => material.GetBytes(index++, count);
}

internal sealed class MemoizedEncryptionMaterial(IEncryptionMaterial source) : IEncryptionMaterial
{
    private readonly Dictionary<(int Index, int Count), byte[]> values = [];

    public byte[] GetBytes(int index, int count)
    {
        if (!values.TryGetValue((index, count), out var known))
        {
            known = source.GetBytes(index, count);
            values[(index, count)] = known;
        }

        var result = new byte[known.Length];
        Array.Copy(known, result, known.Length);
        return result;
    }
}

internal sealed class CapturedEncryptionMaterial : IEncryptionMaterial
{
    private static readonly int[] RequestSizes = [4, 8, 16, 32];
    private readonly Dictionary<(int Index, int Count), byte[]> values = [];

    public CapturedEncryptionMaterial(IEncryptionMaterial source, int requestLimit)
    {
        for (var index = 0; index < requestLimit; index++)
        {
            foreach (var count in RequestSizes)
            {
                var value = source.GetBytes(index, count);
                if (value.Length != count)
                {
                    throw new InvalidOperationException(
                        $"Encryption material returned {value.Length} bytes for a request of {count} bytes.");
                }

                var captured = new byte[count];
                Array.Copy(value, captured, count);
                values[(index, count)] = captured;
            }
        }
    }

    public byte[] GetBytes(int index, int count)
    {
        if (!values.TryGetValue((index, count), out var captured))
        {
            throw new InvalidOperationException("The rendered document did not materialize enough encryption data.");
        }

        var result = new byte[captured.Length];
        Array.Copy(captured, result, captured.Length);
        return result;
    }
}
