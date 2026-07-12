#nullable enable
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Container.Opacity and Image.Opacity paint their draws through a page /ExtGState
// (/ca fill, /CA stroke) selected with the gs operator; Section.Watermark stamps a
// rotated, semi-transparent overlay centered on every page of the section. All of it
// is additive: default opacity and no watermark emit no ExtGState and no gs.
public class OpacityWatermarkTests
{
    private static Paragraph Text(string text)
    {
        var paragraph = new Paragraph();
        paragraph.Inlines.Add(text);
        return paragraph;
    }

    private static string PageText(DocumentBuilder builder, int index = 0)
    {
        var reader = BuildTestSupport.Read(builder);
        var (page, _) = BuildTestSupport.PageLeaves(reader)[index];
        return Encoding.ASCII.GetString(BuildTestSupport.Content(reader, page));
    }

    private static DictionaryObject? ExtGStates(DocumentBuilder builder, int index = 0)
    {
        var reader = BuildTestSupport.Read(builder);
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
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Background = Color.FromRgb(200, 200, 200),
            Opacity = 0.5,
        });
        container.Blocks.Add(Text("Boxed"));

        var states = ExtGStates(builder);
        Assert.NotNull(states);
        Assert.Equal(0.5, Alpha(states!, "GS0", "ca"), 6);
        Assert.Equal(0.5, Alpha(states!, "GS0", "CA"), 6);

        var text = PageText(builder);
        var gs = text.IndexOf("/GS0 gs", System.StringComparison.Ordinal);
        Assert.True(gs >= 0);
        var fill = text.IndexOf(" re f", gs, System.StringComparison.Ordinal);
        Assert.True(fill > gs);
    }

    [Fact]
    public void ContainerOpacity_AppliesToBorders()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var container = section.Blocks.Add(new Container { Opacity = 0.4 });
        container.Borders.Width = 2;
        container.Blocks.Add(Text("Bordered"));

        // Four border edges plus the child text draw share the container's opacity.
        var text = PageText(builder);
        Assert.Equal(5, BuildTestSupport.CountOccurrences(text, "/GS0 gs"));
    }

    [Fact]
    public void ImageOpacity_RegistersExtGStateAndWrapsDrawInGs()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var image = section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));
        image.Opacity = 0.25;

        var states = ExtGStates(builder);
        Assert.NotNull(states);
        Assert.Equal(0.25, Alpha(states!, "GS0", "ca"), 6);

        var text = PageText(builder);
        var gs = text.IndexOf("/GS0 gs", System.StringComparison.Ordinal);
        Assert.True(gs >= 0);
        Assert.True(text.IndexOf(" Do", gs, System.StringComparison.Ordinal) > gs);
    }

    [Fact]
    public void DistinctOpacities_DedupByValue()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.Blocks.Add(new Container { Background = Color.FromRgb(255, 0, 0), Opacity = 0.5 })
            .Blocks.Add(Text("A"));
        section.Blocks.Add(new Container { Background = Color.FromRgb(0, 255, 0), Opacity = 0.5 })
            .Blocks.Add(Text("B"));
        section.Blocks.Add(new Container { Background = Color.FromRgb(0, 0, 255), Opacity = 0.3 })
            .Blocks.Add(Text("C"));

        var states = ExtGStates(builder);
        Assert.NotNull(states);
        Assert.Equal(2, states!.Keys.Count);
        Assert.Equal(0.5, Alpha(states!, "GS0", "ca"), 6);
        Assert.Equal(0.3, Alpha(states!, "GS1", "ca"), 6);

        // Each container wraps its background fill and its child text draw.
        var text = PageText(builder);
        Assert.Equal(4, BuildTestSupport.CountOccurrences(text, "/GS0 gs"));
        Assert.Equal(2, BuildTestSupport.CountOccurrences(text, "/GS1 gs"));
    }

    [Fact]
    public void DefaultOpacity_NoWatermark_EmitsNoExtGStateAndNoGs()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var container = section.Blocks.Add(new Container { Background = Color.FromRgb(200, 200, 200) });
        container.Blocks.Add(Text("Plain"));
        section.Blocks.AddImage(PdfTestResources.Open("Images/rgb.jpg"));

        Assert.Null(ExtGStates(builder));
        Assert.DoesNotContain(" gs\n", PageText(builder), System.StringComparison.Ordinal);
    }

    [Fact]
    public void SectionWatermark_StampsRotatedTextOnEveryPage()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(200));
        section.Watermark = new Watermark { Text = "DRAFT", Opacity = 0.25, Rotation = 45 };
        for (var i = 0; i < 12; i++)
        {
            section.Blocks.Add(Text($"Paragraph {i}"));
        }

        var reader = BuildTestSupport.Read(builder);
        var pages = BuildTestSupport.PageLeaves(reader);
        Assert.True(pages.Count > 1);

        for (var i = 0; i < pages.Count; i++)
        {
            var text = PageText(builder, i);
            Assert.Contains("0.707 0.707 -0.707 0.707 200 100 cm", text, System.StringComparison.Ordinal);
            Assert.Contains("(DRAFT) Tj", text, System.StringComparison.Ordinal);
            Assert.Contains("/GS0 gs", text, System.StringComparison.Ordinal);

            var states = ExtGStates(builder, i);
            Assert.NotNull(states);
            Assert.Equal(0.25, Alpha(states!, "GS0", "ca"), 6);
        }
    }

    [Fact]
    public void SectionWatermark_CentersTextOnTheRotatedOrigin()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.Watermark = new Watermark { Text = "X", Rotation = 0, Opacity = 1 };
        section.Blocks.Add(Text("Body"));

        var text = PageText(builder);
        var width = new FontCollection().MeasureText("X", section.Watermark.Font);
        var index = text.IndexOf("(X) Tj", System.StringComparison.Ordinal);
        Assert.True(index >= 0);
        var expected = string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            "{0:0.###} {1:0.###} Td",
            -width / 2,
            -section.Watermark.Font.Size * 0.35);
        Assert.Contains(expected, text, System.StringComparison.Ordinal);
        Assert.DoesNotContain(" gs\n", text, System.StringComparison.Ordinal);
    }

    [Fact]
    public void SectionWatermark_StampsImageCenteredOnThePage()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        var watermark = new Watermark { Opacity = 0.2, Rotation = 30 };
        watermark.SetImage(PdfTestResources.Open("Images/rgb.jpg"));
        section.Watermark = watermark;
        section.Blocks.Add(Text("Body"));

        var reader = BuildTestSupport.Read(builder);
        Assert.Single(BuildTestSupport.ImageXObjects(reader));

        var text = PageText(builder);
        var gs = text.IndexOf("/GS0 gs", System.StringComparison.Ordinal);
        Assert.True(gs >= 0);
        Assert.Contains("0.866 0.5 -0.5 0.866", text, System.StringComparison.Ordinal);
        Assert.True(text.IndexOf(" Do", gs, System.StringComparison.Ordinal) > gs);
    }
}
