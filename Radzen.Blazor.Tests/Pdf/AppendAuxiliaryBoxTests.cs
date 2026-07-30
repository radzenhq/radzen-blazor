#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class AppendAuxiliaryBoxTests
{
    private static byte[] LoadedBytes(string pageExtra)
    {
        var content = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td (loaded) Tj ET");
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R "
            + pageExtra + " >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + content.Length + " >>\nstream\n").Append(content).Append("\nendstream\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 5\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 5; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }
        pdf.Append("trailer\n<< /Size 5 /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static DictionaryObject Kid(DocumentReader reader, int index)
    {
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]!));
        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Pages"]));
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(pages["Kids"]));
        return Assert.IsType<DictionaryObject>(reader.Resolve(kids[index]));
    }

    private static void AssertBox(DocumentReader reader, DictionaryObject page, string key, double l, double b, double r, double t)
    {
        var box = Assert.IsType<ArrayObject>(reader.Resolve(page[key]));
        Assert.Equal(l, Assert.IsType<NumberObject>(box[0]).DoubleValue);
        Assert.Equal(b, Assert.IsType<NumberObject>(box[1]).DoubleValue);
        Assert.Equal(r, Assert.IsType<NumberObject>(box[2]).DoubleValue);
        Assert.Equal(t, Assert.IsType<NumberObject>(box[3]).DoubleValue);
    }

    [Fact]
    public void Append_LoadedPage_KeepsBleedTrimAndArtBoxes()
    {
        var target = new Document();
        target.Pages.Add().SetContent(Encoding.ASCII.GetBytes("own-page"));
        target.Append(Document.LoadFromStream(new MemoryStream(
            LoadedBytes("/BleedBox [1 2 3 4] /TrimBox [5 6 7 8] /ArtBox [9 10 11 12]"))));

        var reader = DocumentReader.Parse(target.ToArray());
        var appended = Kid(reader, 1);
        AssertBox(reader, appended, "BleedBox", 1, 2, 3, 4);
        AssertBox(reader, appended, "TrimBox", 5, 6, 7, 8);
        AssertBox(reader, appended, "ArtBox", 9, 10, 11, 12);
    }

    [Fact]
    public void Append_GeneratedPage_KeepsRotateAndPrintBoxes()
    {
        var source = new Document();
        var page = source.Pages.Add();
        page.Rotate = 90;
        page.TrimBox = new PdfRect(5, 6, 7, 8);
        page.BleedBox = new PdfRect(1, 2, 3, 4);

        var target = new Document();
        target.Pages.Add().SetContent(Encoding.ASCII.GetBytes("own-page"));
        target.Append(source);

        var reader = DocumentReader.Parse(target.ToArray());
        var appended = Kid(reader, 1);
        Assert.Equal(90, Assert.IsType<NumberObject>(reader.Resolve(appended["Rotate"])).IntValue);
        AssertBox(reader, appended, "TrimBox", 5, 6, 7, 8);
        AssertBox(reader, appended, "BleedBox", 1, 2, 3, 4);
    }
}
