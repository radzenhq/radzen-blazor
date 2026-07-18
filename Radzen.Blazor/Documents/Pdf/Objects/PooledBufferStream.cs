using System;
using System.IO;

namespace Radzen.Documents.Pdf.Objects;

internal sealed class PooledBufferStream(int initialCapacity = 4 * 1024) : Stream
{
    private readonly PooledByteAccumulator accumulator = new(initialCapacity);

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => accumulator.Length;

    public override long Position
    {
        get => accumulator.Length;
        set => throw new NotSupportedException();
    }

    public ReadOnlySpan<byte> WrittenSpan => accumulator.WrittenSpan;

    public byte[] ToArray() => accumulator.ToArray();

    public override void WriteByte(byte value) => accumulator.Append(value);

    public override void Write(byte[] buffer, int offset, int count) => accumulator.Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> source) => accumulator.Write(source);

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            accumulator.Return();
        }

        base.Dispose(disposing);
    }
}
