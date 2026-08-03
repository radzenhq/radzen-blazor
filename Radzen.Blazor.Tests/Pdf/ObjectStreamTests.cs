#nullable enable
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 7.5.7 object streams.
public class ObjectStreamTests
{
    private static byte[] QpdfFixture() =>
        PdfTestResources.ReadAllBytes("Documents/xref-stream-objstm.pdf");

    [Fact]
    public void QpdfFixture_ObjectCountCountsInUseIncludingObjStmMembers()
    {
        Assert.Equal(7, DocumentReader.Parse(QpdfFixture()).ObjectCount);
    }

    [Fact]
    public void QpdfFixture_Type2EntriesExtractCorrectTypesAndValues()
    {
        var reader = DocumentReader.Parse(QpdfFixture());

        var catalog = Assert.IsType<DictionaryObject>(reader.GetObject(2));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);

        var pages = Assert.IsType<DictionaryObject>(reader.GetObject(3));
        Assert.Equal("Pages", Assert.IsType<NameObject>(pages["Type"]).Value);
        Assert.Equal(1, Assert.IsType<NumberObject>(pages["Count"]).IntValue);

        var page = Assert.IsType<DictionaryObject>(reader.GetObject(4));
        Assert.Equal("Page", Assert.IsType<NameObject>(page["Type"]).Value);

        var font = Assert.IsType<DictionaryObject>(reader.GetObject(5));
        Assert.Equal("Font", Assert.IsType<NameObject>(font["Type"]).Value);
        Assert.Equal("Helvetica", Assert.IsType<NameObject>(font["BaseFont"]).Value);
        Assert.Equal("Type1", Assert.IsType<NameObject>(font["Subtype"]).Value);
    }

    [Fact]
    public void QpdfFixture_ObjStmItselfIsAStreamObject()
    {
        var reader = DocumentReader.Parse(QpdfFixture());
        var objStm = Assert.IsType<StreamObject>(reader.GetObject(1));
        Assert.Equal("ObjStm", Assert.IsType<NameObject>(objStm.Dictionary["Type"]).Value);
        Assert.Equal(4, Assert.IsType<NumberObject>(objStm.Dictionary["N"]).IntValue);
    }

    private static byte[] HandCraftedObjStmFile()
    {
        var b1 = "42";
        var b2 = "<< /A 1 /B (x) >>";
        var b3 = "<< /Type /Catalog >>";
        var body = b1 + "\n" + b2 + "\n" + b3;
        var o1 = 0;
        var o2 = (b1 + "\n").Length;
        var o3 = (b1 + "\n" + b2 + "\n").Length;
        var header = $"1 {o1} 2 {o2} 3 {o3} ";
        var stmData = header + body;
        var first = header.Length;
        var length = stmData.Length;

        var pdf = new FixturePdf().Append("%PDF-1.5\n");
        var offset4 = pdf.Position;
        pdf.Append($"4 0 obj\n<< /Type /ObjStm /N 3 /First {first} /Length {length} >>\nstream\n")
            .Append(stmData)
            .Append("\nendstream\nendobj\n");

        var offset5 = pdf.Position;
        var payload = new byte[20];
        Copy(payload, 0, FixturePdf.XrefStreamEntry(2, 4, 0));
        Copy(payload, 4, FixturePdf.XrefStreamEntry(2, 4, 1));
        Copy(payload, 8, FixturePdf.XrefStreamEntry(2, 4, 2));
        Copy(payload, 12, FixturePdf.XrefStreamEntry(1, (int)offset4, 0));
        Copy(payload, 16, FixturePdf.XrefStreamEntry(1, (int)offset5, 0));

        pdf.Append("5 0 obj\n<< /Type /XRef /Size 6 /Index [1 5] /W [1 2 1] /Root 3 0 R /Length 20 >>\nstream\n")
            .Append(payload)
            .Append("\nendstream\nendobj\n")
            .Append("startxref\n" + offset5 + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void HandCraftedObjStm_ExtractsIntAndDicts()
    {
        var reader = DocumentReader.Parse(HandCraftedObjStmFile());

        Assert.Equal(42, Assert.IsType<NumberObject>(reader.GetObject(1)).IntValue);

        var dict = Assert.IsType<DictionaryObject>(reader.GetObject(2));
        Assert.Equal(1, Assert.IsType<NumberObject>(dict["A"]).IntValue);
        Assert.Equal("x", Assert.IsType<StringObject>(dict["B"]).Value);

        var catalog = Assert.IsType<DictionaryObject>(reader.GetObject(3));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
    }

    [Fact]
    public void HandCraftedObjStm_TrailerRootResolvesThroughType2Entry()
    {
        var reader = DocumentReader.Parse(HandCraftedObjStmFile());
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
    }

    private static byte[] ObjStmWithMemberOffset(int memberOffset)
    {
        var header = $"1 {memberOffset} ";
        var body = "42";
        var stmData = header + body;
        var first = header.Length;

        var pdf = new FixturePdf().Append("%PDF-1.5\n");
        var offset4 = pdf.Position;
        pdf.Append($"4 0 obj\n<< /Type /ObjStm /N 1 /First {first} /Length {stmData.Length} >>\nstream\n")
            .Append(stmData)
            .Append("\nendstream\nendobj\n");

        var offset5 = pdf.Position;
        var payload = new byte[12];
        Copy(payload, 0, FixturePdf.XrefStreamEntry(2, 4, 0));
        Copy(payload, 4, FixturePdf.XrefStreamEntry(1, (int)offset4, 0));
        Copy(payload, 8, FixturePdf.XrefStreamEntry(1, (int)offset5, 0));

        pdf.Append("5 0 obj\n<< /Type /XRef /Size 6 /Index [1 1 4 2] /W [1 2 1] /Root 4 0 R /Length 12 >>\nstream\n")
            .Append(payload)
            .Append("\nendstream\nendobj\n")
            .Append("startxref\n" + offset5 + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void ObjStmNegativeMemberOffset_ThrowsDocumentParseException()
    {
        var reader = DocumentReader.Parse(ObjStmWithMemberOffset(-100000));
        var exception = Record.Exception(() => reader.GetObject(1));
        Assert.IsType<DocumentParseException>(exception);
    }

    [Fact]
    public void ObjStmMemberNumberMismatch_Throws()
    {
        var b0 = "42";
        var b1 = "<< /Type /Catalog >>";
        var body = b0 + "\n" + b1;
        var off1 = (b0 + "\n").Length;

        var header = $"99 0 2 {off1} ";
        var stmData = header + body;
        var first = header.Length;

        var pdf = new FixturePdf().Append("%PDF-1.5\n");
        var offStm = pdf.Position;
        pdf.Append($"4 0 obj\n<< /Type /ObjStm /N 2 /First {first} /Length {stmData.Length} >>\nstream\n")
            .Append(stmData)
            .Append("\nendstream\nendobj\n");

        var offXref = pdf.Position;
        var payload = new byte[16];
        Copy(payload, 0, FixturePdf.XrefStreamEntry(2, 4, 0));
        Copy(payload, 4, FixturePdf.XrefStreamEntry(2, 4, 1));
        Copy(payload, 8, FixturePdf.XrefStreamEntry(1, (int)offStm, 0));
        Copy(payload, 12, FixturePdf.XrefStreamEntry(1, (int)offXref, 0));
        pdf.Append("5 0 obj\n<< /Type /XRef /Size 6 /Index [1 2 4 2] /W [1 2 1] /Root 2 0 R /Length 16 >>\nstream\n")
            .Append(payload)
            .Append("\nendstream\nendobj\n")
            .Append("startxref\n" + offXref + "\n%%EOF\n");

        var reader = DocumentReader.Parse(pdf.ToArray());

        var catalog = Assert.IsType<DictionaryObject>(reader.GetObject(2));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);

        Assert.Throws<DocumentParseException>(() => reader.GetObject(1));
    }

    private static void Copy(byte[] target, int at, byte[] source)
    {
        for (var i = 0; i < source.Length; i++)
        {
            target[at + i] = source[i];
        }
    }
}
