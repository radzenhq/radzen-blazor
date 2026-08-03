using System;
using System.Collections.Generic;

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
