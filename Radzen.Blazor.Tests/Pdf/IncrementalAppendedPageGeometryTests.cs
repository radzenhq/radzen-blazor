#nullable enable
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class IncrementalAppendedPageGeometryTests
{
    private static byte[] ShiftedPage()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [20 20 612 812] "
                + "/CropBox [30 30 600 800] /Rotate 90 /Contents 4 0 R >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Length 12 >>\nstream\n0 0 10 10 re\nendstream\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 5\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number < 5; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static byte[] BaseDocument()
    {
        var document = new Document();
        document.Info.Title = "Base";
        document.Pages.Add(PageSizes.A4).SetContent(System.Text.Encoding.ASCII.GetBytes("BT (base) Tj ET"));
        return document.ToArray();
    }

    private static Document Load(byte[] bytes) => Document.LoadFromStream(new MemoryStream(bytes));

    private static double N(ArrayObject box, int i) => Assert.IsType<NumberObject>(box[i]).DoubleValue;

    private static DictionaryObject AppendedPage(byte[] saved)
    {
        var reader = DocumentReader.Parse(saved);
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Pages"]));
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(pages["Kids"]));
        return Assert.IsType<DictionaryObject>(reader.Resolve(kids[^1]));
    }

    [Fact]
    public void SaveIncremental_PreservesAppendedPagePreservedBoxes()
    {
        var document = Load(BaseDocument());
        document.Append(Load(ShiftedPage()));

        using var stream = new MemoryStream();
        document.SaveIncremental(stream);
        var page = AppendedPage(stream.ToArray());

        var reader = DocumentReader.Parse(stream.ToArray());
        var media = Assert.IsType<ArrayObject>(reader.Resolve(page["MediaBox"]));
        Assert.Equal(20, N(media, 0), 0.01);
        Assert.Equal(20, N(media, 1), 0.01);
        Assert.Equal(612, N(media, 2), 0.01);
        Assert.Equal(812, N(media, 3), 0.01);

        var crop = Assert.IsType<ArrayObject>(reader.Resolve(page["CropBox"]));
        Assert.Equal(30, N(crop, 0), 0.01);
        Assert.Equal(600, N(crop, 2), 0.01);

        Assert.Equal(90, Assert.IsType<NumberObject>(reader.Resolve(page["Rotate"])).DoubleValue, 0.01);
    }

    [Fact]
    public void SaveToStream_PreservesAppendedPagePreservedBoxes()
    {
        var document = Load(BaseDocument());
        document.Append(Load(ShiftedPage()));

        var page = AppendedPage(document.ToArray());
        var reader = DocumentReader.Parse(document.ToArray());
        var media = Assert.IsType<ArrayObject>(reader.Resolve(page["MediaBox"]));
        Assert.Equal(20, N(media, 0), 0.01);
        Assert.Equal(812, N(media, 3), 0.01);
    }
}
