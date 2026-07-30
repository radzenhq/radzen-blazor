#nullable enable
using System;
using System.IO;
using System.Text;
using Xunit;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Radzen.Documents;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

public class PdfBoundaryValidationTests
{
    private sealed class LyingLengthStream : Stream
    {
        private readonly byte[] data;
        private int position;
        private readonly long claimedLength;

        public LyingLengthStream(byte[] data, long claimedLength)
        {
            this.data = data;
            this.claimedLength = claimedLength;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => claimedLength;
        public override long Position { get => position; set => position = (int)value; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var n = Math.Min(count, data.Length - position);
            if (n <= 0)
            {
                return 0;
            }

            Array.Copy(data, position, buffer, offset, n);
            position += n;
            return n;
        }

        public override long Seek(long offset, SeekOrigin origin) => position;
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override void Flush() { }
    }

    // ISO 32000-1 7.3.3 has no token for a non-finite number.
    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void WriteNumber_NonFinite_Throws(double value)
    {
        var writer = new ContentWriter();

        var exception = Assert.Throws<InvalidOperationException>(() => writer.WriteNumber(value));
        Assert.Equal("A PDF number cannot be NaN or infinite.", exception.Message);
    }

    [Fact]
    public void WriteNumber_NonFinite_MatchesObjectPathMessage()
    {
        var writer = new ContentWriter();
        var content = Assert.Throws<InvalidOperationException>(() => writer.WriteNumber(double.NaN));
        var number = Assert.Throws<InvalidOperationException>(
            () => new NumberObject(double.NaN).Write(new MemoryStream(), new WriteContext()));

        Assert.Equal(number.Message, content.Message);
    }

    [Fact]
    public void WriteNumber_Finite_StillWrites()
    {
        var writer = new ContentWriter();
        writer.WriteNumber(1.23456);

        Assert.Equal("1.235", Encoding.ASCII.GetString(writer.ToArray()));
    }

    [Fact]
    public void Register_StreamShorterThanItsLength_Throws()
    {
        var fonts = new FontCollection();
        var real = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf");
        var stream = new LyingLengthStream(real, claimedLength: real.Length + 500_000);

        Assert.Throws<EndOfStreamException>(() => fonts.Register("Liar", stream));
    }

    [Fact]
    public void Register_HonestStream_StillRegisters()
    {
        var fonts = new FontCollection();
        var real = PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf");

        fonts.Register("Honest", new LyingLengthStream(real, claimedLength: real.Length));

        Assert.True(fonts.MeasureText("Hi", new Font { Family = "Honest", Size = 12 }) > 0);
    }

    // ISO 32000-1 7.4.2: ASCIIHexDecode '>' EOD, but stream /Length bounds the data;
    // contrast the hex-string object reader 7.3.4.3 whose '>' is a required delimiter.
    [Fact]
    public void AsciiHex_MissingEod_IsNotAnError()
        => Assert.Equal("He", Encoding.ASCII.GetString(AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("4865"))));

    [Fact]
    public void AsciiHex_NonHexByte_Throws()
        => Assert.Throws<InvalidDataException>(() => AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("48ZZ>")));
}
