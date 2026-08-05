#nullable enable
using System.Linq;

using Radzen.Documents;
using Xunit;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class PdfPaintOrderTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RenderedPage_PaintsOverlappingTextAndImageInDeclarationOrder(bool imageFirst)
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(200), Unit.FromPoint(200));
        section.Margins.SetAll(Unit.FromPoint(20));
        var overlay = section.Blocks.Add(new Container { Layout = ContainerLayout.Overlay });
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("TEXT");
        var image = new Image(PdfTestResources.ReadAllBytes("Images/rgb.jpg"))
        {
            Width = Unit.FromPoint(80),
            Height = Unit.FromPoint(40),
        };
        if (imageFirst)
        {
            overlay.Blocks.Add(image);
            overlay.Blocks.Add(paragraph);
        }
        else
        {
            overlay.Blocks.Add(paragraph);
            overlay.Blocks.Add(image);
        }

        var operations = Parsed(document);
        var text = operations.FindIndex(operation =>
            operation.Operator == "Tj"
            && System.Text.Encoding.Latin1.GetString(operation.Operands[0].Bytes) == "TEXT");
        var imageDraw = operations.FindIndex(operation => operation.Operator == "Do");

        Assert.True(text >= 0);
        Assert.True(imageDraw >= 0);
        Assert.Equal(imageFirst, imageDraw < text);
    }

    [Fact]
    public void RenderedPage_PaintsTheWatermarkAfterEveryBodyOperator()
    {
        var operations = Parsed(OverlappingPage());
        var watermarkText = operations.FindIndex(operation =>
            operation.Operator == "Tj"
            && System.Text.Encoding.Latin1.GetString(operation.Operands[0].Bytes) == "DRAFT");
        Assert.True(watermarkText >= 0);
        var watermarkTransform = operations.FindLastIndex(
            watermarkText,
            operation => operation.Operator == "cm"
                && operation.Num(4) == 200
                && operation.Num(5) == 200);

        Assert.True(watermarkTransform >= 0);
        Assert.DoesNotContain(
            operations.Skip(watermarkTransform + 1).Take(watermarkText - watermarkTransform - 1),
            operation => operation.Operator is "f" or "S" or "re" or "Do");
        Assert.All(
            operations.Select((operation, index) => (operation, index))
                .Where(entry => entry.operation.Operator is "f" or "S" or "re" or "Do"),
            entry => Assert.True(entry.index < watermarkTransform));
    }

    [Fact]
    public void RenderedPage_ShowsBodyTextBeforeHeaderAndFooterText()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(400));
        section.Margins.SetAll(Unit.FromPoint(40));
        section.Header.Blocks.Add(new Paragraph("HDR"));
        section.Footer.Blocks.Add(new Paragraph("FTR"));
        section.Blocks.Add(new Paragraph("BODY"));

        var shown = ShownStrings(document);

        Assert.Equal(new[] { "BODY", "HDR", "FTR" }, shown);
    }

    private static string[] ShownStrings(Document document)
    {
        var content = ContentTestHelpers.PageContent(BuildTestSupport.Read(document), 0);
        return ContentStreamTokenizer.Parse(content)
            .Where(operation => operation.Operator == "Tj")
            .Select(operation => System.Text.Encoding.Latin1.GetString(operation.Operands[0].Bytes))
            .ToArray();
    }

    private static Document OverlappingPage()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(400));
        section.Margins.SetAll(Unit.FromPoint(20));
        section.Watermark = new Watermark
        {
            Text = "DRAFT",
            Opacity = 0.3,
            Rotation = 45,
            Font = { Family = "Helvetica", Size = 48 },
        };

        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(8),
            Background = Color.FromRgb(200, 220, 240),
            Opacity = 0.8,
            BlendMode = BlendMode.Multiply,
        });
        container.Borders.SetAll(width: 3, color: Color.FromRgb(10, 20, 30));

        var caption = new Paragraph();
        var run = caption.Inlines.Add("Overlapping");
        run.Font.Family = BuildTestSupport.Latin;
        container.Blocks.Add(caption);
        container.Blocks.Add(new Image(PdfTestResources.Open("Images/rgb.jpg")));

        return document;
    }

    private static System.Collections.Generic.List<ContentOperation> Parsed(Document document)
        => ContentStreamTokenizer.Parse(
            ContentTestHelpers.PageContent(BuildTestSupport.Read(document), 0));

}
