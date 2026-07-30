#nullable enable
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 7.3.10: an indirect reference to a free or nonexistent object resolves to null
public class DanglingReferenceTests
{
    private static byte[] FileWithDanglingAnnot()
    {
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
            + "/Resources << >> /Contents 4 0 R /Annots [9 0 R] >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Length 5 >>\nstream\nBT ET\nendstream\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 10\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(1)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(2)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(3)))
            .Append(FixturePdf.Entry20(pdf.OffsetOf(4)))
            .Append(FixturePdf.Entry20(0, 0, 'f'))
            .Append(FixturePdf.Entry20(0, 0, 'f'))
            .Append(FixturePdf.Entry20(0, 0, 'f'))
            .Append(FixturePdf.Entry20(0, 0, 'f'))
            .Append(FixturePdf.Entry20(0, 0, 'f'))
            .Append("trailer\n<< /Size 10 /Root 1 0 R >>\n")
            .Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void GetObject_FreeEntry_ReturnsNull()
    {
        var reader = DocumentReader.Parse(FileWithDanglingAnnot());
        Assert.IsType<NullObject>(reader.GetObject(9));
    }

    [Fact]
    public void GetObject_NonexistentEntry_ReturnsNull()
    {
        var reader = DocumentReader.Parse(FileWithDanglingAnnot());
        Assert.IsType<NullObject>(reader.GetObject(42));
    }

    [Fact]
    public void Resolve_DanglingReference_YieldsNull()
    {
        var reader = DocumentReader.Parse(FileWithDanglingAnnot());
        Assert.IsType<NullObject>(reader.Resolve(new ReferenceObject(9, 0)));
    }

    [Fact]
    public void Resolve_ValidReference_StillResolves()
    {
        var reader = DocumentReader.Parse(FileWithDanglingAnnot());
        var page = Assert.IsType<DictionaryObject>(reader.Resolve(new ReferenceObject(3, 0)));
        Assert.Equal("Page", Assert.IsType<NameObject>(page["Type"]).Value);
    }

    [Fact]
    public void LoadSaveRoundTrip_WithDanglingAnnot_Succeeds()
    {
        using var input = new MemoryStream(FileWithDanglingAnnot());
        var document = Document.LoadFromStream(input);
        Assert.Equal(1, document.Pages.Count);

        var saved = document.ToArray();

        var reader = DocumentReader.Parse(saved);
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        Assert.Equal("Catalog", Assert.IsType<NameObject>(catalog["Type"]).Value);
    }
}
