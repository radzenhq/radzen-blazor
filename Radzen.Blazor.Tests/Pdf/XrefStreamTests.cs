#nullable enable
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Cross-reference stream contract (ISO 32000-1 7.5.8). DocumentReader.Parse must
// accept a file whose cross-reference section is a /Type /XRef stream instead of
// a classic "xref" table. Field decoding is pinned with a hand-built stream that
// uses no predictor and no filter, so the expectations are independent of qpdf.
public class XrefStreamTests
{
    // qpdf-generated fixture (see `qpdf --show-xref`): object 1 is the ObjStm,
    // objects 2-5 are compressed inside it (2=Catalog, 3=Pages, 4=Page, 5=Font),
    // object 6 is the content stream, object 7 is the /Type /XRef stream itself.
    // Trailer (= the xref stream dict): /Root 2 0 R, /Size 8, /W [1 2 1].
    private static byte[] QpdfFixture() =>
        PdfTestResources.ReadAllBytes("Documents/xref-stream-objstm.pdf");

    [Fact]
    public void QpdfFixture_TrailerIsXrefStreamDict()
    {
        var reader = DocumentReader.Parse(QpdfFixture());

        Assert.Equal("XRef", Assert.IsType<NameObject>(reader.Trailer["Type"]).Value);
        Assert.Equal(8, Assert.IsType<NumberObject>(reader.Trailer["Size"]).IntValue);
        var root = Assert.IsType<ReferenceObject>(reader.Trailer["Root"]);
        Assert.Equal(2, root.ObjectNumber);
    }

    [Fact]
    public void QpdfFixture_ResolvesCatalogPagesPage()
    {
        var reader = DocumentReader.Parse(QpdfFixture());

        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);

        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Pages"]));
        Assert.Equal("Pages", Assert.IsType<NameObject>(pages["Type"]).Value);
        Assert.Equal(1, Assert.IsType<NumberObject>(pages["Count"]).IntValue);

        var kids = Assert.IsType<ArrayObject>(pages["Kids"]);
        var page = Assert.IsType<DictionaryObject>(reader.Resolve(kids[0]));
        Assert.Equal("Page", Assert.IsType<NameObject>(page["Type"]).Value);
    }

    [Fact]
    public void QpdfFixture_PageContentContainsText()
    {
        var reader = DocumentReader.Parse(QpdfFixture());

        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Pages"]));
        var kids = Assert.IsType<ArrayObject>(pages["Kids"]);
        var page = Assert.IsType<DictionaryObject>(reader.Resolve(kids[0]));

        var content = Assert.IsType<StreamObject>(reader.Resolve(page["Contents"]));
        var text = Encoding.Latin1.GetString(FlateFilter.Decode(content.Data.ToArray()));
        Assert.Contains("Hello encrypted world", text);
    }

    // Hand-built minimal xref stream: /W [1 2 1], no /Filter, no predictor, so
    // the stream payload is raw 4-byte entries. /Index [1 4] lists objects 1-4;
    // object 0 is implicitly free. All entries are type 1 (uncompressed).
    private static byte[] RawXrefStreamFile()
    {
        var pdf = new FixturePdf().Append("%PDF-1.5\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R >>\nendobj\n");

        var offset4 = pdf.Position;
        var payload = new byte[16];
        Copy(payload, 0, FixturePdf.XrefStreamEntry(1, (int)pdf.OffsetOf(1), 0));
        Copy(payload, 4, FixturePdf.XrefStreamEntry(1, (int)pdf.OffsetOf(2), 0));
        Copy(payload, 8, FixturePdf.XrefStreamEntry(1, (int)pdf.OffsetOf(3), 0));
        Copy(payload, 12, FixturePdf.XrefStreamEntry(1, (int)offset4, 0));

        pdf.Append("4 0 obj\n<< /Type /XRef /Size 5 /Index [1 4] /W [1 2 1] /Root 1 0 R /Length 16 >>\nstream\n")
            .Append(payload)
            .Append("\nendstream\nendobj\n")
            .Append("startxref\n" + offset4 + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void RawXrefStream_TrailerRootResolves()
    {
        var reader = DocumentReader.Parse(RawXrefStreamFile());

        var root = Assert.IsType<ReferenceObject>(reader.Trailer["Root"]);
        Assert.Equal(1, root.ObjectNumber);
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(root));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
    }

    [Fact]
    public void RawXrefStream_AllType1EntriesResolve()
    {
        var reader = DocumentReader.Parse(RawXrefStreamFile());

        Assert.Equal("Catalog", Assert.IsType<NameObject>(
            Assert.IsType<DictionaryObject>(reader.GetObject(1))["Type"]).Value);
        Assert.Equal("Pages", Assert.IsType<NameObject>(
            Assert.IsType<DictionaryObject>(reader.GetObject(2))["Type"]).Value);
        Assert.Equal("Page", Assert.IsType<NameObject>(
            Assert.IsType<DictionaryObject>(reader.GetObject(3))["Type"]).Value);
    }

    // Hybrid update: the newest section is an xref stream whose /Prev points back
    // to a classic "xref" table (ISO 32000-1 7.5.8 with a classic predecessor).
    // Objects 1-2 live in the classic table; object 3 is added in the increment
    // and is listed (with the xref stream itself, object 4) by the xref stream.
    private static byte[] HybridPrevFile()
    {
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 0 /Kids [] >>\nendobj\n");

        var classic = pdf.Position;
        pdf.Append("xref\n0 3\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(2)))
            .Append("trailer\n<< /Size 3 /Root 1 0 R >>\n");

        var offset3 = pdf.Position;
        pdf.Append("3 0 obj\n(added later)\nendobj\n");

        var offset4 = pdf.Position;
        var payload = new byte[8];
        Copy(payload, 0, FixturePdf.XrefStreamEntry(1, (int)offset3, 0));
        Copy(payload, 4, FixturePdf.XrefStreamEntry(1, (int)offset4, 0));

        pdf.Append("4 0 obj\n<< /Type /XRef /Size 5 /Index [3 2] /W [1 2 1] /Root 1 0 R /Prev "
                + classic + " /Length 8 >>\nstream\n")
            .Append(payload)
            .Append("\nendstream\nendobj\n")
            .Append("startxref\n" + offset4 + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void HybridPrev_ClassicAndStreamObjectsBothResolve()
    {
        var reader = DocumentReader.Parse(HybridPrevFile());

        Assert.Equal("Catalog", Assert.IsType<NameObject>(
            Assert.IsType<DictionaryObject>(reader.GetObject(1))["Type"]).Value);
        Assert.Equal("Pages", Assert.IsType<NameObject>(
            Assert.IsType<DictionaryObject>(reader.GetObject(2))["Type"]).Value);
        Assert.Equal("added later", Assert.IsType<StringObject>(reader.GetObject(3)).Value);
    }

    [Fact]
    public void HybridPrev_TrailerRootResolvesToCatalog()
    {
        var reader = DocumentReader.Parse(HybridPrevFile());

        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
    }

    private static void Copy(byte[] target, int at, byte[] source)
    {
        for (var i = 0; i < source.Length; i++)
        {
            target[at + i] = source[i];
        }
    }
}
