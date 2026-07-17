#nullable enable
using System;
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class DocModelHardeningTests
{
    private static Cell NewCell()
    {
        var table = new Table();
        table.Columns.Add(Unit.FromPoint(100));
        return table.Rows.Add().Cells[0];
    }

    [Fact]
    public void CellText_SetNull_LeavesNoBlocks()
    {
        var cell = NewCell();
        cell.Text = "value";
        Assert.Single(cell.Blocks);

        cell.Text = null;
        Assert.Empty(cell.Blocks);
        Assert.Null(cell.Text);
    }

    [Fact]
    public void InlineCollection_AddNullRun_Throws()
    {
        var inlines = new Paragraph().Inlines;
        Assert.Throws<ArgumentNullException>(() => inlines.Add((Run)null!));
    }

    private static byte[] ValidPdfBytes()
    {
        var document = new Document();
        document.Pages.Add();
        return document.ToArray();
    }

    [Fact]
    public void Load_SeekableStreamOverCap_ThrowsFileSize()
    {
        var bytes = ValidPdfBytes();

        using (var ok = new MemoryStream(bytes))
        {
            Assert.Single(Document.LoadFromStream(ok).Pages);
        }

        var limits = new ReaderLimits { MaxFileBytes = bytes.Length - 1 };
        using var stream = new MemoryStream(bytes);
        var ex = Assert.Throws<DocumentParseException>(() => Document.LoadFromStream(stream, limits));
        Assert.Contains("file size", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_NonSeekableStreamOverCap_ThrowsFileSize()
    {
        var bytes = ValidPdfBytes();
        var limits = new ReaderLimits { MaxFileBytes = bytes.Length - 1 };
        using var stream = new NonSeekableStream(bytes);
        var ex = Assert.Throws<DocumentParseException>(() => Document.LoadFromStream(stream, limits));
        Assert.Contains("file size", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_SeekableStreamShorterThanLength_ThrowsLikeReader()
    {
        var bytes = ValidPdfBytes();

        using var readerStream = new OverReportingStream(bytes, 100);
        Assert.Throws<EndOfStreamException>(() => DocumentReader.Parse(readerStream));

        using var loaderStream = new OverReportingStream(bytes, 100);
        Assert.Throws<EndOfStreamException>(() => Document.LoadFromStream(loaderStream));
    }

    private sealed class OverReportingStream(byte[] data, int overstate) : Stream
    {
        private readonly MemoryStream inner = new(data);

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => inner.Length + overstate;
        public override long Position { get => inner.Position; set => inner.Position = value; }
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class NonSeekableStream(byte[] data) : Stream
    {
        private readonly MemoryStream inner = new(data);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
