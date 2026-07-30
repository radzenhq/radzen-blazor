#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class CxHardeningTests
{
    [Fact]
    public void ContentsArrayExceedingAggregateBudget_Throws()
    {
        var bytes = Pdf(
            (1, "<< /Type /Catalog /Pages 2 0 R >>"),
            (2, "<< /Type /Pages /Kids [3 0 R] /Count 1 >>"),
            (3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents [4 0 R 5 0 R 6 0 R] >>"),
            (4, Stream("1234")),
            (5, Stream("5678")),
            (6, Stream("90ab")));
        var limits = new ReaderLimits { MaxDecodedStreamBytes = 8, MaxAggregateDecodedBytes = 10 };

        Assert.Throws<DocumentParseException>(
            () => Document.LoadFromStream(new MemoryStream(bytes), limits));
    }

    [Fact]
    public void RepairScanExceedingXrefBudget_Throws()
    {
        var pdf = new FixturePdf().Append("%PDF-1.4\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] >>\nendobj\n");
        pdf.Append("startxref\n0\n%%EOF\n");

        Assert.Throws<DocumentParseException>(
            () => DocumentReader.Parse(pdf.ToArray(), null, new ReaderLimits { MaxXrefEntries = 2 }));
    }

    [Fact]
    public void MissingUnrecoverableRoot_Throws()
    {
        var bytes = Pdf(
            (1, "<< /Type /NotCatalog >>"),
            (2, "<< /Type /Pages /Kids [] /Count 0 >>"));

        Assert.Throws<DocumentParseException>(() => Document.LoadFromStream(new MemoryStream(bytes)));
    }

    [Fact]
    public void MistypedRoot_ReconstructsCatalogAndLoadsAllPages()
    {
        var bytes = PdfWithRoot(99,
            (1, "<< /Type /Catalog /Pages 2 0 R >>"),
            (2, "<< /Type /Pages /Kids [3 0 R 4 0 R] /Count 2 >>"),
            (3, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] >>"),
            (4, "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] >>"));

        Assert.Equal(2, Document.LoadFromStream(new MemoryStream(bytes)).Pages.Count);
    }

    [Fact]
    public void ByteArrayOverFileCap_Throws()
    {
        var bytes = Pdf((1, "<< /Type /Catalog /Pages 2 0 R >>"), (2, "<< /Type /Pages /Kids [] /Count 0 >>"));

        Assert.Throws<DocumentParseException>(
            () => DocumentReader.Parse(bytes, null, new ReaderLimits { MaxFileBytes = bytes.Length - 1 }));
    }

    [Fact]
    public void NonSeekableStreamOverFileCap_Throws()
    {
        var bytes = Pdf((1, "<< /Type /Catalog /Pages 2 0 R >>"), (2, "<< /Type /Pages /Kids [] /Count 0 >>"));
        using var stream = new NonSeekableStream(bytes);

        Assert.Throws<DocumentParseException>(
            () => DocumentReader.Parse(stream, null, new ReaderLimits { MaxFileBytes = bytes.Length - 1 }));
    }

    private static string Stream(string content)
        => $"<< /Length {content.Length} >>\nstream\n{content}\nendstream";

    private static byte[] Pdf(params (int Number, string Body)[] objects)
        => PdfWithRoot(1, objects);

    private static byte[] PdfWithRoot(int root, params (int Number, string Body)[] objects)
    {
        var pdf = new FixturePdf().Append("%PDF-1.4\n");
        foreach (var (number, body) in objects)
        {
            pdf.Object(number, $"{number} 0 obj\n{body}\nendobj\n");
        }

        var xref = pdf.Position;
        var max = objects[^1].Number;
        pdf.Append($"xref\n0 {max + 1}\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= max; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        return pdf.Append($"trailer\n<< /Size {max + 1} /Root {root} 0 R >>\nstartxref\n{xref}\n%%EOF\n").ToArray();
    }

    private sealed class NonSeekableStream(byte[] data) : MemoryStream(data)
    {
        public override bool CanSeek => false;
    }
}
