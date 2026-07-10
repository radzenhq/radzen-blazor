#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// R5: round-trip fidelity of generated pages.
// (a) ContentInterpreter must materialize 're' rectangles, 'Do' image XObjects and
//     'v'/'y' curves, and 'Tf' must retain the font resource reference, so a built
//     page survives materialize -> edit -> re-save without dropping fills/images or
//     collapsing text to Helvetica.
// (b) Document.SaveToStream must honor Page.Content edits on a Build() output
//     instead of always writing the generator's original bytes.
// (c) ExtractText on a freshly built document with embedded Type0 fonts must route
//     through the emitted ToUnicode mapping instead of decoding glyph ids as WinAnsi.
public class RoundTripFidelityRegressionTests
{
    private static readonly string[] FillPaints = ["f", "F", "f*", "B", "B*", "b", "b*"];

    // (a) A raw 're ... f' rectangle materializes into a path with geometry that
    // survives a dirty re-save instead of degenerating into an empty fill.
    [Fact]
    public void MaterializedRectangleFill_SurvivesResave()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(Encoding.ASCII.GetBytes("1 0 0 rg\n5 5 100 50 re\nf\n"));

        var path = Assert.IsType<PathContent>(Assert.Single(page.Content));
        Assert.True(path.Fill, "materialized rectangle is a fill");
        path.FillColor = Color.FromRgb(0, 255, 0); // dirty the page so it re-encodes from elements

        var reader = ContentTestHelpers.Reload(document);
        var operations = ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(reader, 0));

        Assert.True(HasFillWithGeometry(operations, 0, 1, 0),
            "re-saved page keeps the recolored fill with rectangle geometry");
    }

    // (a) 'v' and 'y' curve segments materialize and survive a dirty re-save.
    [Fact]
    public void MaterializedCurves_V_And_Y_SurviveResave()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(Encoding.ASCII.GetBytes("0 0 1 RG 1 w\n10 10 m\n20 20 30 30 v\n40 40 50 50 y\nS\n"));

        var path = Assert.IsType<PathContent>(Assert.Single(page.Content));
        path.Thickness = 3; // dirty

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

    // (a) A built cell background ('re f' from the generator) survives
    // load -> materialize -> edit -> re-save.
    [Fact]
    public void BuiltCellBackground_SurvivesMaterializeAndResave()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var row = table.Rows.Add();
        row.Cells[0].Background = Color.Red;
        row.Cells[0].Text = "Total";
        row.Cells[0].Font.Name = "Helvetica";

        var loaded = Load(builder.ToArray());
        DirtyFirstText(loaded.Pages[0]);

        var bytes = loaded.ToArray();
        var reader = DocumentReader.Parse(bytes);
        var operations = ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(reader, 0));

        Assert.True(HasFillWithGeometry(operations, 1, 0, 0),
            "cell background fill keeps its rectangle geometry after materialize + re-save");

        Assert.Contains("Total", Load(bytes).ExtractText(), StringComparison.Ordinal);
    }

    // (a) A built image ('Do' from the generator) survives
    // load -> materialize -> edit -> re-save, and the re-saved page still
    // carries the image XObject the operator references.
    [Fact]
    public void BuiltImage_SurvivesMaterializeAndResave()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(100);
        BuildTestSupport.AddText(section, "Photo caption", "Helvetica");

        var loaded = Load(builder.ToArray());
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

    // (a) Tf keeps the font resource reference: text set in Times must not collapse
    // to Helvetica when the page is materialized, edited and re-saved.
    [Fact]
    public void MaterializedText_KeepsFontResource()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Serif body", "Times");

        var loaded = Load(builder.ToArray());
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

    // (b) Edits to Page.Content on a Build() output are honored on save instead of
    // being silently replaced by the generator's original content.
    [Fact]
    public void BuiltPage_ContentEdit_IsHonoredOnSave()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Original body", "Helvetica");

        var built = builder.Build();
        built.Pages[0].Content.Add(new TextContent("WATERMARK", Unit.FromPoint(72), Unit.FromPoint(400))
        {
            Font = new Font { Name = "Helvetica", Size = 24 },
        });

        var text = Load(built.ToArray()).ExtractText();
        Assert.Contains("WATERMARK", text, StringComparison.Ordinal);
        Assert.Contains("Original body", text, StringComparison.Ordinal);
    }

    // (a)+(b) Stamping a built invoice page must not cost it its backgrounds or
    // images: the edit is saved and the materialized fills/images survive.
    [Fact]
    public void BuiltPage_Stamp_KeepsBackgroundsAndImages()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var table = section.Blocks.AddTable();
        table.Columns.Add();
        var row = table.Rows.Add();
        row.Cells[0].Background = Color.Red;
        row.Cells[0].Text = "Amount";
        row.Cells[0].Font.Name = "Helvetica";
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(80);

        var built = builder.Build();
        built.Pages[0].Content.Add(new TextContent("WATERMARK", Unit.FromPoint(72), Unit.FromPoint(400))
        {
            Font = new Font { Name = "Helvetica", Size = 24 },
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

    // (c) ExtractText on a freshly built document with embedded Type0 fonts decodes
    // through the emitted ToUnicode mapping, matching what a reload extracts.
    [Fact]
    public void BuiltType0_FreshExtractText_ReturnsRealText()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Latin sample", BuildTestSupport.Latin);
        BuildTestSupport.AddText(section, "Здравей свят", BuildTestSupport.Latin);

        var built = builder.Build();
        var fresh = built.ExtractText();
        Assert.Contains("Latin sample", fresh, StringComparison.Ordinal);
        Assert.Contains("Здравей свят", fresh, StringComparison.Ordinal);

        var reloaded = Load(built.ToArray()).ExtractText();
        Assert.Contains("Latin sample", reloaded, StringComparison.Ordinal);
        Assert.Contains("Здравей свят", reloaded, StringComparison.Ordinal);
    }

    private static Document Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return Document.LoadFromStream(stream);
    }

    // Forces the page dirty so BuildContent re-encodes from materialized elements.
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

    // True when some fill paint has path geometry and the given rg fill color.
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
}
