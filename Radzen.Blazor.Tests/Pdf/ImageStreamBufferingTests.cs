#nullable enable

using System;
using System.IO;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Buffering a source stream must cost one copy of the payload, not three. Growing a
// MemoryStream doubles its way to ~2x and ToArray adds a third exact-size buffer, all
// LOH-sized for a photo; a seekable stream's Length sizes the final array up front.
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

    // A non-seekable stream cannot be pre-sized, so it keeps the grow-and-copy path.
    [Fact]
    public void UnseekableStream_StillBuffersFully()
        => Assert.Equal(PayloadBytes, Image.FromStream(new UnseekableStream(new byte[PayloadBytes])).Data.Length);

    // Buffering starts at the stream's current position, so a caller handing over a stream
    // seeked past a container header gets the payload it pointed at, not the whole stream.
    [Fact]
    public void SeekableStream_BuffersFromCurrentPosition()
    {
        var stream = new MemoryStream([1, 2, 3, 4, 5]);
        stream.Position = 2;
        Assert.Equal([3, 4, 5], Image.FromStream(stream).Data);
    }
}
