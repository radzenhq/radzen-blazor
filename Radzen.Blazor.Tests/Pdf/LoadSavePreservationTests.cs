#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class LoadSavePreservationTests
{
    private static PortableDocument LoadSaveReload(byte[] source)
    {
        using var input = new MemoryStream(source);
        var document = PortableDocument.LoadFromStream(input);
        return document;
    }

    private static string PageObject(PortableDocument document)
        => Line(Emit(document), "/Type /Page ");

    private static byte[] Build(string pageExtra, string catalogExtra, string extraObjects, int objectCount)
    {
        var content = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td (page-body) Tj ET");
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R " + catalogExtra + " >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R "
            + pageExtra + " >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + content.Length + " >>\nstream\n").Append(content).Append("\nendstream\nendobj\n");
        if (extraObjects.Length > 0)
        {
            pdf.Append(extraObjects);
        }

        var xref = pdf.Position;
        pdf.Append("xref\n0 " + objectCount + "\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < objectCount; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size " + objectCount + " /Root 1 0 R >>\n")
            .Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void Rotate90_SurvivesLoadSave()
    {
        Carries("page", "/Rotate 90", PageObject(LoadSaveReload(Build("/Rotate 90", "", "", 5))));
    }

    [Fact]
    public void Rotate90_IsReportedByThePage()
    {
        Assert.Equal(90, LoadSaveReload(Build("/Rotate 90", "", "", 5)).Pages[0].Rotate);
    }

    [Fact]
    public void InheritedRotate_IsReportedByThePage()
    {
        Assert.Equal(270, LoadSaveReload(InheritedRotate270()).Pages[0].Rotate);
    }

    [Fact]
    public void NegativeRotate_IsNormalizedToItsPositiveEquivalent()
    {
        var document = LoadSaveReload(Build("/Rotate -90", "", "", 5));

        Assert.Equal(270, document.Pages[0].Rotate);

        Carries("page", "/Rotate 270", PageObject(document));
    }

    [Fact]
    public void RotateSetToZero_RemovesTheLoadedRotation()
    {
        var document = LoadSaveReload(Build("/Rotate 90", "", "", 5));
        document.Pages[0].Rotate = 0;

        Lacks("page", "/Rotate", PageObject(document));
    }

    [Fact]
    public void InheritedRotateSetToZero_RemovesTheLoadedRotation()
    {
        var document = LoadSaveReload(InheritedRotate270());
        document.Pages[0].Rotate = 0;

        Lacks("page", "/Rotate", PageObject(document));
    }

    [Fact]
    public void CropBox_SurvivesLoadSave()
    {
        Carries(
            "page",
            "/CropBox [10 20 200 400]",
            PageObject(LoadSaveReload(Build("/CropBox [10 20 200 400]", "", "", 5))));
    }

    private static byte[] InheritedRotate270()
    {
        var content = Encoding.ASCII.GetBytes("(x) Tj");
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] /Rotate 270 >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n");
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

    [Fact]
    public void InheritedRotate_FromPageTree_SurvivesLoadSave()
    {
        Carries("page", "/Rotate 270", PageObject(LoadSaveReload(InheritedRotate270())));
    }

    [Fact]
    public void NoRotateNoCropBox_StaysUnchanged()
    {
        var page = PageObject(LoadSaveReload(Build("", "", "", 5)));

        Lacks("page", "/Rotate", page);
        Lacks("page", "/CropBox", page);
    }

    [Fact]
    public void CatalogFeatures_OutlinesPageLabelsOpenAction_Preserved()
    {
        var content = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td (page-body) Tj ET");
        const string catalogExtra =
            "/Outlines 5 0 R /OpenAction [3 0 R /Fit] /PageLabels << /Nums [0 << /S /D >>] >> /Lang (en-US)";

        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R " + catalogExtra + " >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + content.Length + " >>\nstream\n").Append(content).Append("\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Type /Outlines /First 6 0 R /Last 6 0 R /Count 1 >>\nendobj\n");
        pdf.Object(6, "6 0 obj\n<< /Title (My Bookmark) /Parent 5 0 R /Dest [3 0 R /Fit] >>\nendobj\n");

        var xref = pdf.Position;
        pdf.Append("xref\n0 7\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 7; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 7 /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");

        var emission = Emit(LoadSaveReload(pdf.ToArray()));
        var catalog = Line(emission, "/Type /Catalog");

        var outlines = IndirectObject(emission, Shaped("catalog", @"/Outlines (\d+) 0 R", catalog).Groups[1].Value);
        var first = IndirectObject(emission, Shaped("outline root", @"/First (\d+) 0 R", outlines).Groups[1].Value);
        Carries("first outline item", "/Title (My Bookmark)", first);

        Carries("catalog", "/PageLabels <<", catalog);
        Carries("catalog", "/Lang (en-US)", catalog);

        var target = IndirectObject(
            emission,
            Shaped("catalog", @"/OpenAction \[(\d+) 0 R /Fit\]", catalog).Groups[1].Value);
        Carries("open action target", "/Type /Page ", target);
    }
}
