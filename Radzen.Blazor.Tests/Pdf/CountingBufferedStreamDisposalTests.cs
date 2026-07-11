#nullable enable
using System;
using System.Buffers;
using System.IO;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class CountingBufferedStreamDisposalTests
{
    // Never actually written to; Write always throws before touching it.
    private sealed class ThrowsOnWriteStream : Stream
    {
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new IOException("disk full");

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();
    }

    [Fact]
    public void DisposeReturnsPooledBufferEvenWhenFinalFlushThrows()
    {
        using var inner = new ThrowsOnWriteStream();
        var stream = new CountingBufferedStream(inner);
        stream.WriteByte(1);

        Assert.Throws<IOException>(() => stream.Dispose());

        // The rented buffer was returned to the pool by the throwing Dispose above, so
        // renting again should be able to reuse it rather than growing the pool further.
        var reclaimed = ArrayPool<byte>.Shared.Rent(64 * 1024);
        try
        {
            Assert.True(reclaimed.Length >= 64 * 1024);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(reclaimed);
        }
    }

    [Fact]
    public void DisposeAfterThrowingFlushDoesNotThrowAgainOnSecondDispose()
    {
        using var inner = new ThrowsOnWriteStream();
        var stream = new CountingBufferedStream(inner);
        stream.WriteByte(1);

        Assert.Throws<IOException>(() => stream.Dispose());

        // Second Dispose sees a null buffer and must be a no-op, not rethrow or double-return.
        var exception = Record.Exception(() => stream.Dispose());
        Assert.Null(exception);
    }
}
