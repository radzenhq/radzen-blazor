using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Radzen.Documents.Internal;

namespace Radzen.Documents.Pdf.Objects;

[SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "The destination stream is owned by the caller.")]
internal sealed class CountingBufferedStream(Stream inner) : WriteOnlyStream
{
    private readonly Stream inner = inner;
    private byte[]? buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
    private int count;
    private long flushed;

    public override long Length => flushed + count;

    public override void WriteByte(byte value)
    {
        var data = buffer ?? throw new ObjectDisposedException(nameof(CountingBufferedStream));
        if (count == data.Length)
        {
            FlushBuffer();
        }

        data[count++] = value;
    }

    public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

    public override void Write(ReadOnlySpan<byte> source)
    {
        var data = buffer ?? throw new ObjectDisposedException(nameof(CountingBufferedStream));
        if (source.Length <= data.Length - count)
        {
            source.CopyTo(data.AsSpan(count));
            count += source.Length;
            return;
        }

        FlushBuffer();
        if (source.Length < data.Length)
        {
            source.CopyTo(data);
            count = source.Length;
        }
        else
        {
            inner.Write(source);
            flushed += source.Length;
        }
    }

    public override void Flush()
    {
        FlushBuffer();
        inner.Flush();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && buffer is not null)
        {
            var rented = buffer;
            try
            {
                FlushBuffer();
            }
            finally
            {
                buffer = null;
                ArrayPool<byte>.Shared.Return(rented);
            }
        }

        base.Dispose(disposing);
    }

    private void FlushBuffer()
    {
        if (count > 0)
        {
            inner.Write(buffer!, 0, count);
            flushed += count;
            count = 0;
        }
    }
}
