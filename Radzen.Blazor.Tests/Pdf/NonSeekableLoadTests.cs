#nullable enable
using System;
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class NonSeekableLoadTests
{
    private static byte[] BuildLargeDocument(int padding)
    {
        var document = new PortableDocument();

        var payload = new byte[padding];
        new Random(11).NextBytes(payload);
        document.Pages.Add(PageSizes.A4).SetContent(payload);

        return document.ToArray();
    }

    private static byte[] ClassicPdf()
    {
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [] /Count 0 >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 3\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(2)))
            .Append("trailer\n<< /Size 3 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void Load_NonSeekableStreamReadsTheSameBytesAsSeekable()
    {
        var bytes = BuildLargeDocument((256 * 1024) + 517);

        using var stream = new NonSeekableStream(bytes, 7000);
        var chunked = PortableDocument.LoadFromStream(stream).ToArray();

        using var seekable = new MemoryStream(bytes);
        Assert.Equal(PortableDocument.LoadFromStream(seekable).ToArray(), chunked);
    }

    [Fact]
    public void NonSeekableStream_ParsesIdenticallyToSeekable()
    {
        var bytes = ClassicPdf();

        var seekable = DocumentReader.Parse(new MemoryStream(bytes));
        var forwardOnly = DocumentReader.Parse(new NonSeekableStream(bytes));

        Assert.Equal(seekable.ObjectCount, forwardOnly.ObjectCount);
        var catalog = Assert.IsType<DictionaryObject>(forwardOnly.Resolve(forwardOnly.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
    }
}
