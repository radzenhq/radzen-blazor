#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

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

    private static string AppendedPage(string emission)
        => IndirectObject(
            emission,
            Shaped("pages node", @"/Kids \[\d+ 0 R (\d+) 0 R", Line(emission, "/Type /Pages ")).Groups[1].Value);

    [Fact]
    public void Append_LoadedPage_KeepsBleedTrimAndArtBoxes()
    {
        var target = new PortableDocument();
        target.Pages.Add().SetContent(Encoding.ASCII.GetBytes("own-page"));
        target.Append(PortableDocument.LoadFromStream(new MemoryStream(
            LoadedBytes("/BleedBox [1 2 3 4] /TrimBox [5 6 7 8] /ArtBox [9 10 11 12]"))));

        var appended = AppendedPage(Emit(target));

        Carries("appended page", "/BleedBox [1 2 3 4]", appended);
        Carries("appended page", "/TrimBox [5 6 7 8]", appended);
        Carries("appended page", "/ArtBox [9 10 11 12]", appended);
    }

    [Fact]
    public void Append_GeneratedPage_KeepsRotateAndPrintBoxes()
    {
        var source = new PortableDocument();
        var page = source.Pages.Add();
        page.Rotate = 90;
        page.TrimBox = new PdfRect(5, 6, 7, 8);
        page.BleedBox = new PdfRect(1, 2, 3, 4);

        var target = new PortableDocument();
        target.Pages.Add().SetContent(Encoding.ASCII.GetBytes("own-page"));
        target.Append(source);

        var appended = AppendedPage(Emit(target));

        Carries("appended page", "/Rotate 90", appended);
        Carries("appended page", "/TrimBox [5 6 7 8]", appended);
        Carries("appended page", "/BleedBox [1 2 3 4]", appended);
    }
}
