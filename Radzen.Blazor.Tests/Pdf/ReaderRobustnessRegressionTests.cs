#nullable enable
using System;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

using Radzen.Documents.Pdf;
namespace Radzen.Blazor.Pdf.Tests;

// Hybrid-reference files (ISO 32000-1 7.5.8.4): /XRefStm is consulted before the classic table.
public class ReaderRobustnessRegressionTests
{
    private static byte[] HybridFileWithFreeMask()
    {
        var pdf = new FixturePdf().Append("%PDF-1.6\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
            + "/Resources << /Font << /F1 4 0 R >> >> >>\nendobj\n");

        var member = "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>";
        var objStmData = "4 0 " + member;
        pdf.Mark(5);
        pdf.Append("5 0 obj\n<< /Type /ObjStm /N 1 /First 4 /Length " + objStmData.Length
            + " >>\nstream\n" + objStmData + "\nendstream\nendobj\n");

        var offset6 = pdf.Position;
        var payload = new byte[12];
        Copy(payload, 0, FixturePdf.XrefStreamEntry(2, 5, 0));
        Copy(payload, 4, FixturePdf.XrefStreamEntry(1, (int)pdf.OffsetOf(5), 0));
        Copy(payload, 8, FixturePdf.XrefStreamEntry(1, (int)offset6, 0));
        pdf.Mark(6);
        pdf.Append("6 0 obj\n<< /Type /XRef /Size 7 /Index [4 3] /W [1 2 1] /Root 1 0 R /Length 12 >>\nstream\n")
            .Append(payload)
            .Append("\nendstream\nendobj\n");

        var xrefOffset = pdf.Position;
        pdf.Append("xref\n0 5\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(2)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(3)))
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append("trailer\n<< /Size 7 /Root 1 0 R /XRefStm " + offset6 + " >>\n")
            .Append("startxref\n" + xrefOffset + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static void Copy(byte[] target, int at, byte[] source)
        => Array.Copy(source, 0, target, at, source.Length);

    [Fact]
    public void HybridXref_ClassicFreeEntry_DoesNotMaskCompressedObject()
    {
        var reader = DocumentReader.Parse(HybridFileWithFreeMask());

        var font = Assert.IsType<DictionaryObject>(reader.GetObject(4));
        Assert.Equal("Helvetica", Assert.IsType<NameObject>(font["BaseFont"]).Value);
    }

    [Fact]
    public void HybridXref_ClassicFreeEntry_FontResolvesThroughGraph()
    {
        var reader = DocumentReader.Parse(HybridFileWithFreeMask());

        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Pages"]));
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(pages["Kids"]));
        var page = Assert.IsType<DictionaryObject>(reader.Resolve(kids[0]));
        var resources = Assert.IsType<DictionaryObject>(reader.Resolve(page["Resources"]));
        var fonts = Assert.IsType<DictionaryObject>(reader.Resolve(resources["Font"]));
        var font = Assert.IsType<DictionaryObject>(reader.Resolve(fonts["F1"]));
        Assert.Equal("Helvetica", Assert.IsType<NameObject>(font["BaseFont"]).Value);
    }

    private static byte[] FileWithStreamLength(string length)
    {
        const string content = "BT /F1 12 Tf 72 720 Td (hello) Tj ET";
        var pdf = new FixturePdf().Append("%PDF-1.4\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Length " + length + " >>\nstream\n" + content + "\nendstream\nendobj\n");

        var xrefOffset = pdf.Position;
        pdf.Append("xref\n0 5\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(2)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(3)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(4)))
            .Append("trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n" + xrefOffset + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Theory]
    [InlineData("-5")]
    [InlineData("-2147483648")]
    [InlineData("999999")]
    public void WrongStreamLength_RecoversTheStreamInsteadOfThrowing(string length)
    {
        var reader = DocumentReader.Parse(FileWithStreamLength(length));

        var recovered = Assert.IsType<StreamObject>(reader.GetObject(4));

        Assert.NotEmpty(recovered.Data.ToArray());
    }

    private static byte[] FileWithBrokenXrefStream()
    {
        var pdf = new FixturePdf().Append("%PDF-1.5\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\n");

        var offset4 = pdf.Position;
        var payload = new byte[] { 1, 0, 0, 0 };
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Type /XRef /Size 5 /Root 1 0 R /Length " + payload.Length + " >>\nstream\n")
            .Append(payload)
            .Append("\nendstream\nendobj\n");

        pdf.Append("startxref\n" + offset4 + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void MalformedXrefStream_MissingW_TriggersRepairScan()
    {
        var reader = DocumentReader.Parse(FileWithBrokenXrefStream());

        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
        var page = Assert.IsType<DictionaryObject>(reader.GetObject(3));
        Assert.Equal("Page", Assert.IsType<NameObject>(page["Type"]).Value);
    }
}
