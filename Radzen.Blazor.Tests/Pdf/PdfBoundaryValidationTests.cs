#nullable enable
using System;
using System.IO;
using System.Text;
using Xunit;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Markdown;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;

namespace Radzen.Blazor.Pdf.Tests;

// Boundary contracts for authoring entry points that previously deferred their failure
// past the point where a bad value had already reached output.
public class PdfBoundaryValidationTests
{
    // A seekable stream whose Length promises more than Read will deliver.
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

    // ISO 32000-1 7.3.3 has no token for a non-finite number. The object path
    // (NumberObject.Write) already rejects these; page content must agree.
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
    public void HeadingFontSizes_Null_ThrowsNamingArgument()
    {
        var exception = Assert.Throws<ArgumentNullException>(
            () => new MarkdownPdfOptions { HeadingFontSizes = null! });

        Assert.Equal("value", exception.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(7)]
    public void HeadingFontSizes_WrongLength_ThrowsNamingArgument(int length)
    {
        var exception = Assert.Throws<ArgumentException>(
            () => new MarkdownPdfOptions { HeadingFontSizes = new double[length] });

        Assert.Equal("value", exception.ParamName);
        Assert.Contains("exactly 6 entries", exception.Message);
    }

    [Fact]
    public void HeadingFontSizes_SixEntries_Accepted()
    {
        var options = new MarkdownPdfOptions { HeadingFontSizes = [30, 20, 15, 12, 10, 8] };

        MarkdownPdf.Render(new BlockCollection(), "# Title", options);

        Assert.Equal(30, options.HeadingFontSizes[0]);
    }

    // FontCollection buffered with CopyTo, which silently accepted a stream that
    // delivered fewer bytes than Length promised. DocumentReader.ReadFully is the single
    // implementation and rejects that rather than parsing a truncated prefix.
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

        Assert.True(fonts.MeasureText("Hi", new Font { Name = "Honest", Size = 12 }) > 0);
    }

    // ISO 32000-1 7.4.2: '>' is the ASCIIHexDecode EOD marker, but stream /Length already
    // bounds the data, so running out is not an error. This is the contract that separates
    // the filter from the hex-string OBJECT reader (Lexer.ReadHexString, 7.3.4.3), whose
    // '>' is a required delimiter and whose absence is "Unterminated".
    [Fact]
    public void AsciiHex_MissingEod_IsNotAnError()
        => Assert.Equal("He", Encoding.ASCII.GetString(AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("4865"))));

    [Fact]
    public void AsciiHex_NonHexByte_Throws()
        => Assert.Throws<InvalidDataException>(() => AsciiHexFilter.Decode(Encoding.ASCII.GetBytes("48ZZ>")));
}
