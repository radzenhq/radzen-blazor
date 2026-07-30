#nullable enable
using System.Collections.Generic;
using System.Linq;

using Radzen.Documents.Pdf.Render;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class PdfPaintOrderTests
{
    [Fact]
    public void PageContentFinalizer_MatchesThePdfPaintOrderContract()
        => Assert.Equal(PdfPaintOrder.Phases, PageContentFinalizer.PaintOrder());

    [Fact]
    public void RenderedPage_PaintsFillThenStrokeThenImageThenTextThenWatermark()
        => Assert.Equal(
            new[] { PaintPhase.Fill, PaintPhase.Stroke, PaintPhase.Image, PaintPhase.Text, PaintPhase.Watermark },
            ObservedPhases(OverlappingPage()));

    [Fact]
    public void RenderedPage_PaintsEveryPhaseDeclaredByThePaintOrderContract()
        => Assert.Equal(PdfPaintOrder.Phases, ObservedPhases(OverlappingPage()));

    [Fact]
    public void RenderedPage_PaintsTheWatermarkAfterEveryBodyOperator()
    {
        var operators = Operators(OverlappingPage());
        var watermark = Assert.Single(operators.Select((op, index) => (op, index)).Where(entry => entry.op == "gs")).index;

        Assert.DoesNotContain(operators.Skip(watermark), op => op is "f" or "S" or "re" or "Do");
        Assert.Contains(operators.Skip(watermark), op => op is "Tj" or "TJ");
    }

    [Fact]
    public void RenderedPage_ShowsBodyTextBeforeHeaderAndFooterText()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(400));
        section.Margins.SetAll(Unit.FromPoint(40));
        section.Header.Blocks.AddParagraph("HDR");
        section.Footer.Blocks.AddParagraph("FTR");
        section.Blocks.AddParagraph("BODY");

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
            Font = { Family = BuildTestSupport.Latin, Size = 48 },
        };

        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(8),
            Background = Color.FromRgb(200, 220, 240),
        });
        container.Borders.Width = 3;
        container.Borders.Color = Color.FromRgb(10, 20, 30);

        var caption = new Paragraph();
        var run = caption.Inlines.Add("Overlapping");
        run.Font.Family = BuildTestSupport.Latin;
        container.Blocks.Add(caption);
        container.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));

        return document;
    }

    private static PaintPhase[] ObservedPhases(Document document)
    {
        var phases = new List<PaintPhase>();
        foreach (var phase in PhaseStream(document))
        {
            if (phases.Count == 0 || phases[^1] != phase)
            {
                phases.Add(phase);
            }
        }

        return phases.ToArray();
    }

    private static IEnumerable<PaintPhase> PhaseStream(Document document)
    {
        var watermark = false;
        foreach (var op in Operators(document))
        {
            if (op == "gs")
            {
                watermark = true;
            }

            if (watermark)
            {
                yield return PaintPhase.Watermark;
                continue;
            }

            var phase = op switch
            {
                "f" or "f*" or "B" => PaintPhase.Fill,
                "S" or "s" => PaintPhase.Stroke,
                "Do" => PaintPhase.Image,
                "Tj" or "TJ" => PaintPhase.Text,
                _ => (PaintPhase?)null,
            };

            if (phase is { } value)
            {
                yield return value;
            }
        }
    }

    private static List<string> Operators(Document document)
        => ContentStreamTokenizer.Operators(
            ContentTestHelpers.PageContent(BuildTestSupport.Read(document), 0));
}
