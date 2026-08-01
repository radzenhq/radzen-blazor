#nullable enable
using System;
using System.Globalization;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class OpacityWatermarkTests
{
    private static Paragraph Text(string text)
    {
        var paragraph = new Paragraph();
        paragraph.Inlines.Add(text);
        return paragraph;
    }

    private static string PageText(Document document, int index = 0)
    {
        var reader = BuildTestSupport.Read(document);
        var (page, _) = BuildTestSupport.PageLeaves(reader)[index];
        return Encoding.ASCII.GetString(BuildTestSupport.Content(reader, page));
    }

    private static DictionaryObject? ExtGStates(Document document, int index = 0)
    {
        var reader = BuildTestSupport.Read(document);
        var (_, resources) = BuildTestSupport.PageLeaves(reader)[index];
        return resources is not null
            && resources.TryGetValue("ExtGState", out var states)
            && reader.Resolve(states!) is DictionaryObject dict
            ? dict
            : null;
    }

    private static double Alpha(DictionaryObject states, string key, string entry)
        => ((NumberObject)((DictionaryObject)states[key]!)[entry]!).DoubleValue;

    [Fact]
    public void ContainerOpacity_RegistersExtGStateAndWrapsFillInGs()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Background = Color.FromRgb(200, 200, 200),
            Opacity = 0.5,
        });
        container.Blocks.Add(Text("Boxed"));

        var states = ExtGStates(document);
        Assert.NotNull(states);
        Assert.Equal(0.5, Alpha(states!, "GS0", "ca"), 6);
        Assert.Equal(0.5, Alpha(states!, "GS0", "CA"), 6);

        var text = PageText(document);
        var gs = text.IndexOf("/GS0 gs", StringComparison.Ordinal);
        Assert.True(gs >= 0);
        var fill = text.IndexOf(" re f", gs, StringComparison.Ordinal);
        Assert.True(fill > gs);
    }

    [Fact]
    public void ContainerOpacity_AppliesToBorders()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container { Opacity = 0.4 });
        container.Borders.Width = 2;
        container.Blocks.Add(Text("Bordered"));

        var text = PageText(document);
        Assert.Equal(5, BuildTestSupport.CountOccurrences(text, "/GS0 gs"));
    }

    [Fact]
    public void ImageOpacity_RegistersExtGStateAndWrapsDrawInGs()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Opacity = 0.25;

        var states = ExtGStates(document);
        Assert.NotNull(states);
        Assert.Equal(0.25, Alpha(states!, "GS0", "ca"), 6);

        var text = PageText(document);
        var gs = text.IndexOf("/GS0 gs", StringComparison.Ordinal);
        Assert.True(gs >= 0);
        Assert.True(text.IndexOf(" Do", gs, StringComparison.Ordinal) > gs);
    }

    [Fact]
    public void DistinctOpacities_DedupByValue()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.Blocks.Add(new Container { Background = Color.FromRgb(255, 0, 0), Opacity = 0.5 })
            .Blocks.Add(Text("A"));
        section.Blocks.Add(new Container { Background = Color.FromRgb(0, 255, 0), Opacity = 0.5 })
            .Blocks.Add(Text("B"));
        section.Blocks.Add(new Container { Background = Color.FromRgb(0, 0, 255), Opacity = 0.3 })
            .Blocks.Add(Text("C"));

        var states = ExtGStates(document);
        Assert.NotNull(states);
        Assert.Equal(2, states!.Keys.Count);
        Assert.Equal(0.5, Alpha(states!, "GS0", "ca"), 6);
        Assert.Equal(0.3, Alpha(states!, "GS1", "ca"), 6);

        var text = PageText(document);
        Assert.Equal(4, BuildTestSupport.CountOccurrences(text, "/GS0 gs"));
        Assert.Equal(2, BuildTestSupport.CountOccurrences(text, "/GS1 gs"));
    }

    [Fact]
    public void DefaultOpacity_NoWatermark_EmitsNoExtGStateAndNoGs()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container { Background = Color.FromRgb(200, 200, 200) });
        container.Blocks.Add(Text("Plain"));
        section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));

        Assert.Null(ExtGStates(document));
        Assert.DoesNotContain(" gs\n", PageText(document), StringComparison.Ordinal);
    }

    [Fact]
    public void SectionWatermark_StampsRotatedTextOnEveryPage()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(200));
        section.Watermark = new Watermark { Text = "DRAFT", Opacity = 0.25, Rotation = 45 };
        for (var i = 0; i < 12; i++)
        {
            section.Blocks.Add(Text($"Paragraph {i}"));
        }

        var reader = BuildTestSupport.Read(document);
        var pages = BuildTestSupport.PageLeaves(reader);
        Assert.True(pages.Count > 1);

        for (var i = 0; i < pages.Count; i++)
        {
            var text = PageText(document, i);
            Assert.Contains("0.707 0.707 -0.707 0.707 200 100 cm", text, StringComparison.Ordinal);
            Assert.Contains("(DRAFT) Tj", text, StringComparison.Ordinal);
            Assert.Contains("/GS0 gs", text, StringComparison.Ordinal);

            var states = ExtGStates(document, i);
            Assert.NotNull(states);
            Assert.Equal(0.25, Alpha(states!, "GS0", "ca"), 6);
        }
    }

    [Fact]
    public void SectionWatermark_CentersTextOnTheRotatedOrigin()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.Watermark = new Watermark { Text = "X", Rotation = 0, Opacity = 1 };
        section.Blocks.Add(Text("Body"));

        var text = PageText(document);
        var width = new FontCollection().MeasureText("X", section.Watermark.Font);
        var index = text.IndexOf("(X) Tj", StringComparison.Ordinal);
        Assert.True(index >= 0);
        var expected = string.Format(
            CultureInfo.InvariantCulture,
            "{0:0.###} {1:0.###} Td",
            -width / 2,
            -section.Watermark.Font.Size!.Value.Point * 0.35);
        Assert.Contains(expected, text, StringComparison.Ordinal);
        Assert.DoesNotContain(" gs\n", text, StringComparison.Ordinal);
    }

    [Fact]
    public void SectionWatermark_StampsImageCenteredOnThePage()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var watermark = new Watermark { Opacity = 0.2, Rotation = 30 };
        watermark.SetImage(PdfTestResources.Open("Images/rgb.jpg"));
        section.Watermark = watermark;
        section.Blocks.Add(Text("Body"));

        var reader = BuildTestSupport.Read(document);
        Assert.Single(BuildTestSupport.ImageXObjects(reader));

        var text = PageText(document);
        var gs = text.IndexOf("/GS0 gs", StringComparison.Ordinal);
        Assert.True(gs >= 0);
        Assert.Contains("0.866 0.5 -0.5 0.866", text, StringComparison.Ordinal);
        Assert.True(text.IndexOf(" Do", gs, StringComparison.Ordinal) > gs);
    }
}
