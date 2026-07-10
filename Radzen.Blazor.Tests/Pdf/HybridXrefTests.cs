#nullable enable
using System;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Hybrid-reference file contract (ISO 32000-1 7.5.8.4): a classic xref table
// whose trailer carries /XRefStm pointing at a cross-reference stream. Acrobat
// writes such files by default; the compressed objects are listed only in the
// cross-reference stream, so a reader that ignores /XRefStm cannot find them.
public class HybridXrefTests
{
    // Objects 1-3 (catalog/pages/page) live in the classic table. Object 4 (the
    // page's font) is compressed in ObjStm 5; objects 4-6 are listed only by the
    // /Type /XRef stream (object 6) referenced from the classic trailer /XRefStm.
    private static byte[] HybridFile()
    {
        var pdf = new FixturePdf().Append("%PDF-1.6\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
            + "/Resources << /Font << /F1 4 0 R >> >> >>\nendobj\n");

        // ObjStm holding object 4. "4 0 " pairs occupy the first 4 bytes.
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
        pdf.Append("xref\n0 4\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(2)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(3)))
            .Append("trailer\n<< /Size 7 /Root 1 0 R /XRefStm " + offset6 + " >>\n")
            .Append("startxref\n" + xrefOffset + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static void Copy(byte[] target, int at, byte[] source)
        => Array.Copy(source, 0, target, at, source.Length);

    [Fact]
    public void XRefStm_CompressedObjectResolves()
    {
        var reader = DocumentReader.Parse(HybridFile());

        var font = Assert.IsType<DictionaryObject>(reader.GetObject(4));
        Assert.Equal("Helvetica", Assert.IsType<NameObject>(font["BaseFont"]).Value);
    }

    [Fact]
    public void XRefStm_PageFontResourceResolvesThroughGraph()
    {
        var reader = DocumentReader.Parse(HybridFile());

        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Pages"]));
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(pages["Kids"]));
        var page = Assert.IsType<DictionaryObject>(reader.Resolve(kids[0]));
        var resources = Assert.IsType<DictionaryObject>(reader.Resolve(page["Resources"]));
        var fonts = Assert.IsType<DictionaryObject>(reader.Resolve(resources["Font"]));
        var font = Assert.IsType<DictionaryObject>(reader.Resolve(fonts["F1"]));
        Assert.Equal("Helvetica", Assert.IsType<NameObject>(font["BaseFont"]).Value);
    }

    // The classic table stays authoritative for the objects it lists.
    [Fact]
    public void XRefStm_ClassicEntriesStillResolve()
    {
        var reader = DocumentReader.Parse(HybridFile());

        Assert.Equal("Catalog", Assert.IsType<NameObject>(
            Assert.IsType<DictionaryObject>(reader.GetObject(1))["Type"]).Value);
        Assert.Equal("Page", Assert.IsType<NameObject>(
            Assert.IsType<DictionaryObject>(reader.GetObject(3))["Type"]).Value);
    }
}
