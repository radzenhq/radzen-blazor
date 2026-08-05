#nullable enable

using System.IO;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class ImageStreamBufferingTests
{
    private const int PayloadBytes = 4 * 1024 * 1024;

    private sealed class UnseekableStream(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;
    }

    [Fact]
    public void UnseekableStream_StillBuffersFully()
        => Assert.Equal(PayloadBytes, new Image(new UnseekableStream(new byte[PayloadBytes])).Data.Length);

    [Fact]
    public void SeekableStream_BuffersFromCurrentPosition()
    {
        var stream = new MemoryStream([1, 2, 3, 4, 5]);
        stream.Position = 2;
        Assert.Equal([3, 4, 5], new Image(stream).Data);
    }
}
