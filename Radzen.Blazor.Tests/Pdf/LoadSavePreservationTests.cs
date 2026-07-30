#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class LoadSavePreservationTests
{
    private static PortableDocument LoadSaveReload(byte[] source)
    {
        using var input = new MemoryStream(source);
        var document = PortableDocument.LoadFromStream(input);
        return document;
    }

    private static DocumentReader SaveAndParse(PortableDocument document)
        => DocumentReader.Parse(document.ToArray());

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

    private static DictionaryObject Catalog(DocumentReader reader)
        => Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]!));

    private static DictionaryObject FirstPage(DocumentReader reader)
    {
        var pages = Assert.IsType<DictionaryObject>(reader.Resolve(Catalog(reader)["Pages"]));
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(pages["Kids"]));
        return Assert.IsType<DictionaryObject>(reader.Resolve(kids[0]));
    }

    [Fact]
    public void Rotate90_SurvivesLoadSave()
    {
        var reader = SaveAndParse(LoadSaveReload(Build("/Rotate 90", "", "", 5)));

        var page = FirstPage(reader);
        Assert.Equal(90, Assert.IsType<NumberObject>(reader.Resolve(page["Rotate"])).IntValue);
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

        var reader = SaveAndParse(document);
        Assert.Equal(270, Assert.IsType<NumberObject>(reader.Resolve(FirstPage(reader)["Rotate"])).IntValue);
    }

    [Fact]
    public void RotateSetToZero_RemovesTheLoadedRotation()
    {
        var document = LoadSaveReload(Build("/Rotate 90", "", "", 5));
        document.Pages[0].Rotate = 0;

        Assert.False(FirstPage(SaveAndParse(document)).ContainsKey("Rotate"));
    }

    [Fact]
    public void InheritedRotateSetToZero_RemovesTheLoadedRotation()
    {
        var document = LoadSaveReload(InheritedRotate270());
        document.Pages[0].Rotate = 0;

        Assert.False(FirstPage(SaveAndParse(document)).ContainsKey("Rotate"));
    }

    [Fact]
    public void CropBox_SurvivesLoadSave()
    {
        var reader = SaveAndParse(LoadSaveReload(Build("/CropBox [10 20 200 400]", "", "", 5)));

        var page = FirstPage(reader);
        var crop = Assert.IsType<ArrayObject>(reader.Resolve(page["CropBox"]));
        Assert.Equal(4, crop.Count);
        Assert.Equal(10.0, Assert.IsType<NumberObject>(crop[0]).DoubleValue);
        Assert.Equal(20.0, Assert.IsType<NumberObject>(crop[1]).DoubleValue);
        Assert.Equal(200.0, Assert.IsType<NumberObject>(crop[2]).DoubleValue);
        Assert.Equal(400.0, Assert.IsType<NumberObject>(crop[3]).DoubleValue);
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
        var reader = SaveAndParse(LoadSaveReload(InheritedRotate270()));
        Assert.Equal(270, Assert.IsType<NumberObject>(reader.Resolve(FirstPage(reader)["Rotate"])).IntValue);
    }

    [Fact]
    public void NoRotateNoCropBox_StaysUnchanged()
    {
        var reader = SaveAndParse(LoadSaveReload(Build("", "", "", 5)));

        var page = FirstPage(reader);
        Assert.False(page.ContainsKey("Rotate"));
        Assert.False(page.ContainsKey("CropBox"));
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

        var reader = SaveAndParse(LoadSaveReload(pdf.ToArray()));
        var catalog = Catalog(reader);

        var outlinesDict = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Outlines"]));
        var first = Assert.IsType<DictionaryObject>(reader.Resolve(outlinesDict["First"]));
        Assert.Equal("My Bookmark", Assert.IsType<StringObject>(reader.Resolve(first["Title"])).Value);

        Assert.True(catalog.ContainsKey("PageLabels"));
        Assert.Equal("en-US", Assert.IsType<StringObject>(reader.Resolve(catalog["Lang"])).Value);

        var openAction = Assert.IsType<ArrayObject>(reader.Resolve(catalog["OpenAction"]));
        var target = Assert.IsType<DictionaryObject>(reader.Resolve(openAction[0]));
        Assert.Equal("Page", Assert.IsType<NameObject>(target["Type"]).Value);
    }
}
