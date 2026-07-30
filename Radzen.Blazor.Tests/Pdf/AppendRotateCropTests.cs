#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Document = Radzen.Documents.Pdf.Document;

namespace Radzen.Blazor.Pdf.Tests;

public class AppendRotateCropTests
{
    private static byte[] Build(string pageExtra)
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

    private static Document Load(string pageExtra)
        => Document.LoadFromStream(new MemoryStream(Build(pageExtra)));

    private static DictionaryObject Kid(DocumentReader reader, int index)
    {
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]!));
        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Pages"]));
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(pages["Kids"]));
        return Assert.IsType<DictionaryObject>(reader.Resolve(kids[index]));
    }

    [Fact]
    public void AppendedLoadedPage_KeepsRotateAndCropBox()
    {
        var target = new Document();
        target.Pages.Add().SetContent(Encoding.ASCII.GetBytes("own-page"));
        target.Append(Load("/Rotate 90 /CropBox [10 20 200 400]"));

        var reader = DocumentReader.Parse(target.ToArray());
        var appended = Kid(reader, 1);

        Assert.Equal(90, Assert.IsType<NumberObject>(reader.Resolve(appended["Rotate"])).IntValue);
        var crop = Assert.IsType<ArrayObject>(reader.Resolve(appended["CropBox"]));
        Assert.Equal(4, crop.Count);
        Assert.Equal(10.0, Assert.IsType<NumberObject>(crop[0]).DoubleValue);
        Assert.Equal(20.0, Assert.IsType<NumberObject>(crop[1]).DoubleValue);
        Assert.Equal(200.0, Assert.IsType<NumberObject>(crop[2]).DoubleValue);
        Assert.Equal(400.0, Assert.IsType<NumberObject>(crop[3]).DoubleValue);
    }

    [Fact]
    public void ChainedAppend_KeepsRotateAndCropBox()
    {
        var b = Load("/Rotate 270 /CropBox [5 5 100 100]");

        var c = new Document();
        c.Append(b);

        var a = new Document();
        a.Pages.Add().SetContent(Encoding.ASCII.GetBytes("own-page"));
        a.Append(c);

        var reader = DocumentReader.Parse(a.ToArray());
        var appended = Kid(reader, 1);

        Assert.Equal(270, Assert.IsType<NumberObject>(reader.Resolve(appended["Rotate"])).IntValue);
        var crop = Assert.IsType<ArrayObject>(reader.Resolve(appended["CropBox"]));
        Assert.Equal(100.0, Assert.IsType<NumberObject>(crop[2]).DoubleValue);
    }

    [Fact]
    public void AppendedPlainLoadedPage_HasNoRotateOrCropBox()
    {
        var target = new Document();
        target.Pages.Add().SetContent(Encoding.ASCII.GetBytes("own-page"));
        target.Append(Load(""));

        var reader = DocumentReader.Parse(target.ToArray());
        var appended = Kid(reader, 1);

        Assert.False(appended.ContainsKey("Rotate"));
        Assert.False(appended.ContainsKey("CropBox"));
    }
}
