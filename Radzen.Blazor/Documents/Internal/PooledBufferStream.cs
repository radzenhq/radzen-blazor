using System;

namespace Radzen.Documents;

internal sealed class PooledBufferStream(int initialCapacity = 4 * 1024) : WriteOnlyStream
{
    private readonly PooledByteAccumulator accumulator = new(initialCapacity);

    public override long Length => accumulator.Length;

    public ReadOnlySpan<byte> WrittenSpan => accumulator.WrittenSpan;

    public byte[] ToArray() => accumulator.ToArray();

    public override void WriteByte(byte value) => accumulator.Append(value);

    public override void Write(byte[] buffer, int offset, int count) => accumulator.Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> source) => accumulator.Write(source);

    public override void Flush()
    {
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            accumulator.Return();
        }

        base.Dispose(disposing);
    }
}
