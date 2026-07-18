#nullable enable

using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Emit;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class BuilderStreamCapTests
{
    private const long Cap = 4096;

    private sealed class UnseekableStream(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;
    }

    private static ReaderLimits Bounded => new() { MaxFileBytes = Cap };

    [Fact]
    public void ImageReadFully_RejectsUnseekableStreamPastCap()
    {
        var stream = new UnseekableStream(new byte[Cap * 4]);
        Assert.Throws<DocumentParseException>(() => ImageDecoder.ReadFully(stream, Bounded));
    }

    [Fact]
    public void FontBufferStream_RejectsUnseekableStreamPastCap()
    {
        var stream = new UnseekableStream(new byte[Cap * 4]);
        Assert.Throws<DocumentParseException>(() => FontCollection.BufferStream(stream, Bounded));
    }

    [Fact]
    public void ImageReadFully_AcceptsStreamWithinCap()
    {
        var payload = new byte[Cap / 2];
        Assert.Equal(payload.Length, ImageDecoder.ReadFully(new UnseekableStream(payload), Bounded).Length);
    }
}
