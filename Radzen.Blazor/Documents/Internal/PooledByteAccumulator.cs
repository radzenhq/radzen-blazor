using System;
using System.Buffers;

namespace Radzen.Documents.Internal;

internal sealed class PooledByteAccumulator(int initialCapacity)
{
    private byte[]? buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
    private int length;

    private byte[] Buffer => buffer ?? throw new ObjectDisposedException(nameof(PooledByteAccumulator));

    public int Length => length;

    public ReadOnlySpan<byte> WrittenSpan => Buffer.AsSpan(0, length);

    public byte[] ToArray() => WrittenSpan.ToArray();

    public void Append(byte value)
    {
        var data = Buffer;
        if (length == data.Length)
        {
            data = Grow(1);
        }

        data[length++] = value;
    }

    public void Write(ReadOnlySpan<byte> source)
    {
        var data = Buffer;
        if (source.Length > data.Length - length)
        {
            data = Grow(source.Length);
        }

        source.CopyTo(data.AsSpan(length));
        length += source.Length;
    }

    public Span<byte> Reserve(int size)
    {
        var data = Buffer;
        if (data.Length - length < size)
        {
            data = Grow(size);
        }

        return data.AsSpan(length, size);
    }

    public void Advance(int count) => length += count;

    public void Return()
    {
        if (buffer is not null)
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
            buffer = null;
        }
    }

    private byte[] Grow(int extra)
    {
        var data = Buffer;
        var replacement = ArrayPool<byte>.Shared.Rent(Capacity(data.Length, extra));
        data.AsSpan(0, length).CopyTo(replacement);
        ArrayPool<byte>.Shared.Return(data, clearArray: true);
        buffer = replacement;
        return replacement;
    }

    private int Capacity(int current, int extra)
    {
        var required = (long)length + extra;
        if (required > Array.MaxLength)
        {
            throw new InvalidOperationException(
                $"A pooled byte accumulator cannot grow to {required} bytes; the maximum is {Array.MaxLength}.");
        }

        return (int)Math.Min(Math.Max((long)current * 2, required), Array.MaxLength);
    }
}
