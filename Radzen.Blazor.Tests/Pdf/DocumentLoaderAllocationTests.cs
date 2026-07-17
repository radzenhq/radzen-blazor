#nullable enable
using System;
using System.IO;
using System.Runtime.CompilerServices;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class DocumentLoaderAllocationTests
{
    private static byte[] BuildLargeDocument(int padding)
    {
        var document = new Document();

        var payload = new byte[padding];
        new Random(11).NextBytes(payload);
        document.Pages.Add(PageSizes.A4).SetContent(payload);

        return document.ToArray();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Measure(Func<Stream> open)
    {
        using var stream = open();
        var before = GC.GetAllocatedBytesForCurrentThread();
        Document.LoadFromStream(stream);
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }

    [Fact]
    public void Load_NonSeekableStreamDoesNotDoubleBufferTheFile()
    {
        var bytes = BuildLargeDocument(8 * 1024 * 1024);

        Measure(() => new MemoryStream(bytes));
        Measure(() => new NonSeekableStream(bytes));

        var seekable = Measure(() => new MemoryStream(bytes));
        var nonSeekable = Measure(() => new NonSeekableStream(bytes));

        var overhead = nonSeekable - seekable;
        var budget = bytes.Length * 3L / 2L;
        Assert.True(
            overhead < budget,
            $"Non-seekable load allocated {overhead} bytes over the seekable load of the same "
            + $"{bytes.Length}-byte document, budget {budget}.");
    }

    [Fact]
    public void Load_NonSeekableStreamReadsTheSameBytesAsSeekable()
    {
        var bytes = BuildLargeDocument((256 * 1024) + 517);

        using var stream = new NonSeekableStream(bytes, 7000);
        var chunked = Document.LoadFromStream(stream).ToArray();

        using var seekable = new MemoryStream(bytes);
        Assert.Equal(Document.LoadFromStream(seekable).ToArray(), chunked);
    }

    private sealed class NonSeekableStream(byte[] data, int maxRead = int.MaxValue) : Stream
    {
        private readonly MemoryStream inner = new(data);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, Math.Min(count, maxRead));
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
