#nullable enable
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class OverlayContainerTaggingConformanceTests
{
    private static (Document Document, DocumentRenderer Renderer) Author(bool ua, PdfAConformance conformance, ContainerLayout layout, double rotation)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        document.Info.Title = "Boxed";
        var builderRenderer = new DocumentRenderer();
        builderRenderer.Accessibility = ua ? PdfUaConformance.PdfUa1 : PdfUaConformance.None;
        builderRenderer.Conformance = conformance;
        if (ua)
        {
            document.Language = "en-US";
        }

        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container { Padding = Unit.FromPoint(8), Layout = layout, Rotation = rotation });
        container.Blocks.AddParagraph().Inlines.Add("OVERLAID").Font.Family = BuildTestSupport.Latin;
        return (document, builderRenderer);
    }

    private static List<ContentOperation> Ops((Document Document, DocumentRenderer Renderer) authored)
        => ContentStreamTokenizer.Parse(ContentTestHelpers.PageContent(
            BuildTestSupport.Read(authored.Document, authored.Renderer), 0));

    private static byte[] Rendered((Document Document, DocumentRenderer Renderer) authored)
        => authored.Renderer.ToArray(authored.Document);

    private static (Document Document, DocumentRenderer Renderer) Layered(bool tagged)
    {
        var document = new Document { Language = "en-US" };
        BuildTestSupport.RegisterLatin(document);
        document.Info.Title = "Layered";
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container { Layout = ContainerLayout.Overlay });
        container.Blocks.AddParagraph().Inlines.Add("ABOVE IMAGE").Font.Family = BuildTestSupport.Latin;
        var image = container.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Width = Unit.FromPoint(80);
        image.Height = Unit.FromPoint(30);
        image.AlternateText = "Meaningful image";

        return (
            document,
            new DocumentRenderer
            {
                Accessibility = tagged ? PdfUaConformance.PdfUa1 : PdfUaConformance.None,
            });
    }

    private static (Document Document, DocumentRenderer Renderer) TableAndFigure(bool tagged)
    {
        var document = new Document { Language = "en-US" };
        BuildTestSupport.RegisterLatin(document);
        document.Info.Title = "TableAndFigure";
        var section = document.Sections.Add();

        var table = section.Blocks.AddTable();
        table.Columns.Add(Unit.FromPoint(120));
        table.Columns.Add(Unit.FromPoint(120));
        var header = table.Rows.Add();
        header.Cells[0].Blocks.AddParagraph().Inlines.Add("LEFT").Font.Family = BuildTestSupport.Latin;
        header.Cells[1].Blocks.AddParagraph().Inlines.Add("RIGHT").Font.Family = BuildTestSupport.Latin;
        var body = table.Rows.Add();
        body.Cells[0].Blocks.AddParagraph().Inlines.Add("ONE").Font.Family = BuildTestSupport.Latin;
        var cellImage = body.Cells[1].Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        cellImage.Width = Unit.FromPoint(60);
        cellImage.Height = Unit.FromPoint(24);
        cellImage.AlternateText = "Cell figure";

        var figure = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        figure.Width = Unit.FromPoint(90);
        figure.Height = Unit.FromPoint(36);
        figure.AlternateText = "Standalone figure";

        return (
            document,
            new DocumentRenderer
            {
                Accessibility = tagged ? PdfUaConformance.PdfUa1 : PdfUaConformance.None,
            });
    }

    private static List<string> DrawOperators((Document Document, DocumentRenderer Renderer) authored)
        => Ops(authored)
            .Where(operation => operation.Operator is not ("BDC" or "BMC" or "EMC" or "DP" or "MP"))
            .Select(operation => operation.Operator)
            .ToList();

    private static HashSet<string> TagsWrappingText(List<ContentOperation> ops)
    {
        var stack = new List<string>();
        var tags = new HashSet<string>();
        foreach (var operation in ops)
        {
            switch (operation.Operator)
            {
                case "BDC" or "BMC":
                    stack.Add(operation.Operands.Count > 0 ? operation.Operands[0].Text : "");
                    break;
                case "EMC":
                    if (stack.Count > 0)
                    {
                        stack.RemoveAt(stack.Count - 1);
                    }

                    break;
                case "Tj" or "TJ" or "'" or "\"":
                    foreach (var tag in stack)
                    {
                        tags.Add(tag);
                    }

                    break;
            }
        }

        return tags;
    }

    [Fact]
    public void PdfUA_OverlayContainerParagraph_IsTaggedNotArtifact()
    {
        var tags = TagsWrappingText(Ops(Author(ua: true, PdfAConformance.None, ContainerLayout.Overlay, 0)));
        Assert.DoesNotContain("Artifact", tags);
        Assert.Contains("P", tags);
    }

    [Fact]
    public void LevelA_OverlayContainerParagraph_IsTaggedNotArtifact()
    {
        var tags = TagsWrappingText(Ops(Author(ua: false, PdfAConformance.PdfA3A, ContainerLayout.Overlay, 0)));
        Assert.DoesNotContain("Artifact", tags);
        Assert.Contains("P", tags);
    }

    [Fact]
    public void PdfUA_RotatedOverlayContainerParagraph_IsTaggedNotArtifact()
    {
        var tags = TagsWrappingText(Ops(Author(ua: true, PdfAConformance.None, ContainerLayout.Overlay, 30)));
        Assert.DoesNotContain("Artifact", tags);
        Assert.Contains("P", tags);
    }

    [Fact]
    public void PlainDocument_OverlayContainerOutputIsByteStable()
    {
        var a = Rendered(Author(ua: false, PdfAConformance.None, ContainerLayout.Overlay, 0));
        var b = Rendered(Author(ua: false, PdfAConformance.None, ContainerLayout.Overlay, 0));
        Assert.Equal(a, b);
    }

    [Fact]
    public void Tagging_DoesNotChangeTheOperatorSequenceOutsideMarkedContent()
    {
        var plain = DrawOperators(Layered(tagged: false));
        var tagged = DrawOperators(Layered(tagged: true));

        Assert.Contains("Do", plain);
        Assert.Contains(plain, operation => operation is "Tj" or "TJ");
        Assert.Equal(plain, tagged);
    }

    [Fact]
    public void Tagging_DoesNotChangeTheOperatorSequenceForTableAndFigureContent()
    {
        var plain = DrawOperators(TableAndFigure(tagged: false));
        var tagged = DrawOperators(TableAndFigure(tagged: true));

        Assert.Equal(2, plain.Count(operation => operation == "Do"));
        Assert.Contains(plain, operation => operation is "Tj" or "TJ");
        Assert.Equal(plain, tagged);
    }
}
