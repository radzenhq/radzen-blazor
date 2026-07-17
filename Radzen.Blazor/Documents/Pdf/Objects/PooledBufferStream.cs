using System;
using System.Buffers;
using System.IO;

namespace Radzen.Documents.Pdf.Objects;

internal sealed class PooledBufferStream(int initialCapacity = 4 * 1024) : Stream
{
    private byte[]? buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
    private int written;

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => written;

    public override long Position
    {
        get => written;
        set => throw new NotSupportedException();
    }

    public ReadOnlySpan<byte> WrittenSpan => Data.AsSpan(0, written);

    private byte[] Data => buffer ?? throw new ObjectDisposedException(nameof(PooledBufferStream));

    public byte[] ToArray() => WrittenSpan.ToArray();

    public override void WriteByte(byte value)
    {
        var data = Data;
        if (written == data.Length)
        {
            data = Grow(1);
        }

        data[written++] = value;
    }

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> source)
    {
        var data = Data;
        if (source.Length > data.Length - written)
        {
            data = Grow(source.Length);
        }

        source.CopyTo(data.AsSpan(written));
        written += source.Length;
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing && buffer is not null)
        {
            ArrayPool<byte>.Shared.Return(buffer);
            buffer = null;
        }

        base.Dispose(disposing);
    }

    private byte[] Grow(int extra)
    {
        var data = Data;
        var replacement = ArrayPool<byte>.Shared.Rent(Math.Max(data.Length * 2, written + extra));
        data.AsSpan(0, written).CopyTo(replacement);
        ArrayPool<byte>.Shared.Return(data);
        buffer = replacement;
        return replacement;
    }
}
