using System;
using System.Buffers;
using System.IO;

namespace Radzen.Documents.Pdf.Objects;


// Write-only stream that batches small writes into a pooled buffer before
// forwarding them to the destination, while tracking the total byte count so
// callers can record offsets without an intermediate full-file copy.
[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "The destination stream is owned by the caller.")]
internal sealed class CountingBufferedStream(Stream inner) : Stream
{
    private readonly Stream inner = inner;
    private byte[]? buffer = ArrayPool<byte>.Shared.Rent(64 * 1024);
    private int count;
    private long flushed;

    public override bool CanRead => false;

    public override bool CanSeek => false;

    public override bool CanWrite => true;

    public override long Length => flushed + count;

    public override long Position
    {
        get => flushed + count;
        set => throw new NotSupportedException();
    }

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

    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();

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
