#nullable enable

using System;
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ImageStreamBufferingTests
{
    private const int PayloadBytes = 4 * 1024 * 1024;

    private static long BufferBytes(Stream stream)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var image = Image.FromStream(stream);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(PayloadBytes, image.Data.Length);
        return allocated;
    }

    private sealed class UnseekableStream(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;
    }

    [Fact]
    public void SeekableStream_BuffersInOneCopy()
    {
        var allocated = BufferBytes(new MemoryStream(new byte[PayloadBytes]));
        Assert.True(allocated < PayloadBytes * 1.5, $"allocated {allocated} bytes for a {PayloadBytes} byte payload");
    }

    [Fact]
    public void UnseekableStream_StillBuffersFully()
        => Assert.Equal(PayloadBytes, Image.FromStream(new UnseekableStream(new byte[PayloadBytes])).Data.Length);

    [Fact]
    public void SeekableStream_BuffersFromCurrentPosition()
    {
        var stream = new MemoryStream([1, 2, 3, 4, 5]);
        stream.Position = 2;
        Assert.Equal([3, 4, 5], Image.FromStream(stream).Data);
    }
}
