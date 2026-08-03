#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class RoundTripFidelityRegressionTests
{
    private static readonly string[] FillPaints = ["f", "F", "f*", "B", "B*", "b", "b*"];

    [Fact]
    public void MaterializedRectangleFill_SurvivesResave()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.SetContent(Encoding.ASCII.GetBytes("1 0 0 rg\n5 5 100 50 re\nf\n"));

        var path = Assert.IsType<PathContent>(Assert.Single(page.Content));
        Assert.True(path.Fill, "materialized rectangle is a fill");
        path.FillColor = Color.FromRgb(0, 255, 0);

        var reader = ContentTestHelpers.Reload(document);
        var operations = ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(reader, 0));

        Assert.True(HasFillWithGeometry(operations, 0, 1, 0),
            "re-saved page keeps the recolored fill with rectangle geometry");
    }

    [Fact]
    public void MaterializedCurves_V_And_Y_SurviveResave()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.SetContent(Encoding.ASCII.GetBytes("0 0 1 RG 1 w\n10 10 m\n20 20 30 30 v\n40 40 50 50 y\nS\n"));

        var path = Assert.IsType<PathContent>(Assert.Single(page.Content));
        path.Thickness = 3;

        var reader = ContentTestHelpers.Reload(document);
        var operations = ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(reader, 0));

        var curves = 0;
        var coordinates = new List<double>();
        foreach (var operation in operations)
        {
            if (operation.Operator is "c" or "v" or "y")
            {
                curves++;
                for (var i = 0; i < operation.Operands.Count; i++)
                {
                    coordinates.Add(operation.Num(i));
                }
            }
        }

        Assert.True(curves >= 2, $"both curve segments survive, found {curves}");
        Assert.Contains(coordinates, value => Math.Abs(value - 30) < 0.01);
        Assert.Contains(coordinates, value => Math.Abs(value - 50) < 0.01);
    }

    [Fact]
    public void BuiltCellBackground_SurvivesMaterializeAndResave()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var row = table.Rows.Add();
        row.Cells[0].Background = Color.Red;
        row.Cells[0].Text = "Total";
        row.Cells[0].Font.Family = "Helvetica";

        var loaded = Load(new DocumentRenderer().ToArray(document));
        DirtyFirstText(loaded.Pages[0]);

        var bytes = loaded.ToArray();
        var reader = DocumentReader.Parse(bytes);
        var operations = ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(reader, 0));

        Assert.True(HasFillWithGeometry(operations, 1, 0, 0),
            "cell background fill keeps its rectangle geometry after materialize + re-save");

        Assert.Contains("Total", Load(bytes).ExtractText(), StringComparison.Ordinal);
    }

    [Fact]
    public void BuiltImage_SurvivesMaterializeAndResave()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(100);
        BuildTestSupport.AddText(section, "Photo caption", "Helvetica");

        var loaded = Load(new DocumentRenderer().ToArray(document));
        DirtyFirstText(loaded.Pages[0]);

        var reader = DocumentReader.Parse(loaded.ToArray());
        var operations = ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(reader, 0));

        var name = FindLastNameOperand(operations, "Do");
        Assert.False(name is null, "re-saved page keeps a Do operator");

        var xobjects = XObjects(reader, 0);
        Assert.True(xobjects is not null && xobjects.ContainsKey(name!),
            $"page /XObject resources contain '{name}' referenced by Do");
        var stream = Assert.IsType<StreamObject>(reader.Resolve(xobjects![name!]));
        Assert.Equal("Image", ((NameObject)reader.Resolve(stream.Dictionary["Subtype"])).Value);
    }

    [Fact]
    public void MaterializedText_KeepsFontResource()
    {
        var document = new Document();
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Serif body", "Times");

        var loaded = Load(new DocumentRenderer().ToArray(document));
        DirtyFirstText(loaded.Pages[0]);

        var bytes = loaded.ToArray();
        var reader = DocumentReader.Parse(bytes);
        var operations = ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(reader, 0));

        var name = FindLastNameOperand(operations, "Tf");
        Assert.False(name is null, "re-saved page has a Tf with a font resource name");

        var font = ContentTestHelpers.FontResource(reader, 0, name!);
        Assert.Equal("Times-Roman", ((NameObject)reader.Resolve(font["BaseFont"])).Value);

        Assert.Contains("Serif body", Load(bytes).ExtractText(), StringComparison.Ordinal);
    }

    [Fact]
    public void BuiltPage_ContentEdit_IsHonoredOnSave()
    {
        var document = new Document();
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Original body", "Helvetica");

        var built = new DocumentRenderer().Render(document);
        built.Pages[0].Content.Add(new TextContent("WATERMARK", Unit.FromPoint(72), Unit.FromPoint(400))
        {
            Font = new Font { Family = "Helvetica", Size = 24 },
        });

        var text = Load(built.ToArray()).ExtractText();
        Assert.Contains("WATERMARK", text, StringComparison.Ordinal);
        Assert.Contains("Original body", text, StringComparison.Ordinal);
    }

    [Fact]
    public void BuiltPage_Stamp_KeepsBackgroundsAndImages()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var row = table.Rows.Add();
        row.Cells[0].Background = Color.Red;
        row.Cells[0].Text = "Amount";
        row.Cells[0].Font.Family = "Helvetica";
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(80);

        var built = new DocumentRenderer().Render(document);
        built.Pages[0].Content.Add(new TextContent("WATERMARK", Unit.FromPoint(72), Unit.FromPoint(400))
        {
            Font = new Font { Family = "Helvetica", Size = 24 },
        });

        var bytes = built.ToArray();
        var reader = DocumentReader.Parse(bytes);
        var operations = ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(reader, 0));

        var text = Load(bytes).ExtractText();
        Assert.Contains("WATERMARK", text, StringComparison.Ordinal);
        Assert.Contains("Amount", text, StringComparison.Ordinal);

        Assert.True(HasFillWithGeometry(operations, 1, 0, 0),
            "stamped built page keeps the red cell background");
        Assert.False(FindLastNameOperand(operations, "Do") is null,
            "stamped built page keeps its image Do operator");
    }

    [Fact]
    public void BuiltType0_FreshExtractText_ReturnsRealText()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Latin sample", BuildTestSupport.Latin);
        BuildTestSupport.AddText(section, "Здравей свят", BuildTestSupport.Latin);

        var built = new DocumentRenderer().Render(document);
        var fresh = built.ExtractText();
        Assert.Contains("Latin sample", fresh, StringComparison.Ordinal);
        Assert.Contains("Здравей свят", fresh, StringComparison.Ordinal);

        var reloaded = Load(built.ToArray()).ExtractText();
        Assert.Contains("Latin sample", reloaded, StringComparison.Ordinal);
        Assert.Contains("Здравей свят", reloaded, StringComparison.Ordinal);
    }

    private static PortableDocument Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return PortableDocument.LoadFromStream(stream);
    }

    private static void DirtyFirstText(Page page)
    {
        foreach (var element in page.Content)
        {
            if (element is TextContent text)
            {
                text.Color = Color.FromRgb(0, 0, 128);
                return;
            }
        }

        Assert.Fail("page has no materialized text element to dirty");
    }

    private static bool HasFillWithGeometry(List<ContentOperation> operations, double r, double g, double b)
    {
        var geometry = 0;
        var color = new[] { 0.0, 0.0, 0.0 };
        foreach (var operation in operations)
        {
            switch (operation.Operator)
            {
                case "m" or "l" or "c" or "v" or "y" or "re":
                    geometry++;
                    break;
                case "rg" when operation.Operands.Count >= 3:
                    color = [operation.Num(0), operation.Num(1), operation.Num(2)];
                    break;
                case "S" or "s" or "n":
                    geometry = 0;
                    break;
                default:
                    if (Array.IndexOf(FillPaints, operation.Operator) >= 0)
                    {
                        if (geometry > 0
                            && Math.Abs(color[0] - r) < 0.02
                            && Math.Abs(color[1] - g) < 0.02
                            && Math.Abs(color[2] - b) < 0.02)
                        {
                            return true;
                        }

                        geometry = 0;
                    }

                    break;
            }
        }

        return false;
    }

    private static string? FindLastNameOperand(List<ContentOperation> operations, string op)
    {
        foreach (var operation in operations)
        {
            if (operation.Operator != op)
            {
                continue;
            }

            for (var i = operation.Operands.Count - 1; i >= 0; i--)
            {
                if (operation.Operands[i].Kind == ContentTokenKind.Name)
                {
                    return operation.Operands[i].Text;
                }
            }
        }

        return null;
    }

    private static DictionaryObject? XObjects(DocumentReader reader, int pageIndex)
    {
        var page = ContentTestHelpers.Kid(reader, pageIndex);
        if (!page.TryGetValue("Resources", out var resourcesObject)
            || reader.Resolve(resourcesObject!) is not DictionaryObject resources
            || !resources.TryGetValue("XObject", out var xo)
            || reader.Resolve(xo!) is not DictionaryObject xobjects)
        {
            return null;
        }

        return xobjects;
    }

    private static DocumentReader SaveAndParse(PortableDocument document)
        => DocumentReader.Parse(document.ToArray());

    private static DictionaryObject Catalog(DocumentReader reader)
        => Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]!));

    private static string Name(DocumentReader reader, DictionaryObject dictionary, string key)
        => Assert.IsType<NameObject>(reader.Resolve(dictionary[key])).Value;

    private static byte[] BuildWithInfo()
    {
        var content = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td (body) Tj ET");
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + content.Length + " >>\nstream\n").Append(content).Append("\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Producer (Acme Producer) /CreationDate (D:20200102030405Z) /ModDate (D:20210304050607+02'00') >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 6\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 6; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 6 /Root 1 0 R /Info 5 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void InfoProducerAndDates_SurviveLoadSave()
    {
        var reader = SaveAndParse(Load(BuildWithInfo()));

        Assert.True(reader.Trailer.TryGetValue("Info", out var infoObject));
        var info = Assert.IsType<DictionaryObject>(reader.Resolve(infoObject!));
        Assert.Equal("Acme Producer", Assert.IsType<StringObject>(reader.Resolve(info["Producer"])).Value);

        var created = Assert.IsType<StringObject>(reader.Resolve(info["CreationDate"])).Value;
        Assert.StartsWith("D:20200102030405", created, StringComparison.Ordinal);
        var modified = Assert.IsType<StringObject>(reader.Resolve(info["ModDate"])).Value;
        Assert.StartsWith("D:20210304050607", modified, StringComparison.Ordinal);
    }

    [Fact]
    public void InfoProducer_CallerOverrideWins()
    {
        var document = Load(BuildWithInfo());
        document.Info.Producer = "Overridden";

        var reader = SaveAndParse(document);
        var info = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Info"]!));
        Assert.Equal("Overridden", Assert.IsType<StringObject>(reader.Resolve(info["Producer"])).Value);
    }

    private static byte[] BuildWithEmbeddedFile()
    {
        var xml = Encoding.UTF8.GetBytes("<invoice/>");
        var content = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td (body) Tj ET");
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /Names 6 0 R /AF [7 0 R] >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + content.Length + " >>\nstream\n").Append(content).Append("\nendstream\nendobj\n");
        pdf.Mark(5);
        pdf.Append("5 0 obj\n<< /Type /EmbeddedFile /Subtype /text#2Fxml /Length " + xml.Length
            + " /Params << /Size " + xml.Length + " /ModDate (D:20200101000000Z) >> >>\nstream\n")
            .Append(xml).Append("\nendstream\nendobj\n");
        pdf.Object(6, "6 0 obj\n<< /EmbeddedFiles << /Names [(factur-x.xml) 7 0 R] >> >>\nendobj\n");
        pdf.Object(7, "7 0 obj\n<< /Type /Filespec /F (factur-x.xml) /UF (factur-x.xml) /AFRelationship /Data /Desc (invoice) /EF << /F 5 0 R /UF 5 0 R >> >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 8\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 8; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 8 /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void EmbeddedFileAndAF_SurviveLoadSave()
    {
        var reader = SaveAndParse(Load(BuildWithEmbeddedFile()));
        var catalog = Catalog(reader);

        Assert.True(catalog.TryGetValue("AF", out var afObject), "catalog has /AF");
        var af = Assert.IsType<ArrayObject>(reader.Resolve(afObject!));
        var filespec = Assert.IsType<DictionaryObject>(reader.Resolve(af[0]));
        Assert.Equal("factur-x.xml", Assert.IsType<StringObject>(reader.Resolve(filespec["F"])).Value);
        Assert.Equal("Data", Name(reader, filespec, "AFRelationship"));

        var names = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Names"]));
        var tree = Assert.IsType<DictionaryObject>(reader.Resolve(names["EmbeddedFiles"]));
        var pairs = Assert.IsType<ArrayObject>(reader.Resolve(tree["Names"]));
        Assert.Equal("factur-x.xml", Assert.IsType<StringObject>(reader.Resolve(pairs[0])).Value);

        var ef = Assert.IsType<DictionaryObject>(reader.Resolve(filespec["EF"]));
        var stream = Assert.IsType<StreamObject>(reader.Resolve(ef["F"]));
        Assert.Equal(Encoding.UTF8.GetBytes("<invoice/>"), reader.DecodeStream(stream));
    }

    private static DictionaryObject HelveticaFont() => new()
    {
        ["Type"] = new NameObject("Font"),
        ["Subtype"] = new NameObject("Type1"),
        ["BaseFont"] = new NameObject("Helvetica"),
        ["Encoding"] = new NameObject("WinAnsiEncoding"),
    };

    [Fact]
    public void TjDisplacement_SurvivesInterpretReEmit()
    {
        var original = Encoding.ASCII.GetBytes("BT /F1 12 Tf 72 700 Td [(He) -120 (llo)] TJ ET\n");
        var document = ExtractionSupport.BuildSinglePage(_ => HelveticaFont(), original);

        TextContent? run = null;
        foreach (var element in document.Pages[0].Content)
        {
            if (element is TextContent text)
            {
                run = text;
                break;
            }
        }

        Assert.NotNull(run);
        run!.Color = Color.Red;

        var bytes = document.ToArray();
        var content = ContentTestHelpers.PageContent(DocumentReader.Parse(bytes), 0);

        Assert.Contains(
            ContentStreamTokenizer.Parse(content),
            operation => operation.Operator == "TJ"
                && operation.Operands.Exists(operand =>
                    operand.Kind == ContentTokenKind.Number && operand.Number == -120));
        Assert.Contains("Hello", Load(bytes).ExtractText(), StringComparison.Ordinal);
    }
}
