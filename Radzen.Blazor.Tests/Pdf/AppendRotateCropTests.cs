#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

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

    private static PortableDocument Load(string pageExtra)
        => PortableDocument.LoadFromStream(new MemoryStream(Build(pageExtra)));

    private static string AppendedPage(string emission)
    {
        var pages = Shaped("catalog", @"/Pages (\d+) 0 R", Line(emission, "/Type /Catalog"));
        var kids = References("page tree", "Kids", 2, IndirectObject(emission, pages.Groups[1].Value));
        return IndirectObject(emission, kids[1]);
    }

    [Fact]
    public void AppendedLoadedPage_KeepsRotateAndCropBox()
    {
        var target = new PortableDocument();
        target.Pages.Add().SetContent(Encoding.ASCII.GetBytes("own-page"));
        target.Append(Load("/Rotate 90 /CropBox [10 20 200 400]"));

        var appended = AppendedPage(Emit(target));

        Carries("appended page", "/Rotate 90", appended);
        Carries("appended page", "/CropBox [10 20 200 400]", appended);
    }

    [Fact]
    public void ChainedAppend_KeepsRotateAndCropBox()
    {
        var b = Load("/Rotate 270 /CropBox [5 5 100 100]");

        var c = new PortableDocument();
        c.Append(b);

        var a = new PortableDocument();
        a.Pages.Add().SetContent(Encoding.ASCII.GetBytes("own-page"));
        a.Append(c);

        var appended = AppendedPage(Emit(a));

        Carries("appended page", "/Rotate 270", appended);
        var crop = Shaped(
            "appended page",
            @"/CropBox \[(-?[\d.]+) (-?[\d.]+) (-?[\d.]+) (-?[\d.]+)\]",
            appended);
        Assert.Equal("100", crop.Groups[3].Value);
    }

    [Fact]
    public void AppendedPlainLoadedPage_HasNoRotateOrCropBox()
    {
        var target = new PortableDocument();
        target.Pages.Add().SetContent(Encoding.ASCII.GetBytes("own-page"));
        target.Append(Load(""));

        var appended = AppendedPage(Emit(target));

        Lacks("appended page", "/Rotate", appended);
        Lacks("appended page", "/CropBox", appended);
    }
}
