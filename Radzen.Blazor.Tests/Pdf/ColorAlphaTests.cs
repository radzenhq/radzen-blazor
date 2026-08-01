#nullable enable
using System;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class ColorAlphaTests
{
    private static Paragraph Text(string text)
    {
        var paragraph = new Paragraph();
        paragraph.Inlines.Add(text);
        return paragraph;
    }

    private static string PageText(Document document)
    {
        var reader = BuildTestSupport.Read(document);
        var (page, _) = BuildTestSupport.PageLeaves(reader)[0];
        return Encoding.ASCII.GetString(BuildTestSupport.Content(reader, page));
    }

    private static DictionaryObject? ExtGStates(Document document)
    {
        var reader = BuildTestSupport.Read(document);
        var (_, resources) = BuildTestSupport.PageLeaves(reader)[0];
        return resources is not null
            && resources.TryGetValue("ExtGState", out var states)
            && reader.Resolve(states!) is DictionaryObject dict
            ? dict
            : null;
    }

    private static double Alpha(DictionaryObject states, string key, string entry)
        => ((NumberObject)((DictionaryObject)states[key]!)[entry]!).DoubleValue;

    private static Document TextColor(Color color)
    {
        var document = new Document();
        var section = document.Sections.Add();
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("Faded").Font.Color = color;
        section.Blocks.Add(paragraph);
        return document;
    }

    [Fact]
    public void TranslucentFontColor_RegistersExtGStateAndWrapsShowInGs()
    {
        var document = TextColor(Color.FromArgb(128, 255, 0, 0));

        var states = ExtGStates(document);
        Assert.NotNull(states);
        Assert.Equal(128 / 255.0, Alpha(states!, "GS0", "ca"), 6);
        Assert.Equal(128 / 255.0, Alpha(states!, "GS0", "CA"), 6);

        var text = PageText(document);
        var gs = text.IndexOf("/GS0 gs", StringComparison.Ordinal);
        Assert.True(gs >= 0);
        Assert.True(text.IndexOf(" Tj", gs, StringComparison.Ordinal) > gs);
    }

    [Fact]
    public void TransparentFontColor_PaintsNothing()
    {
        var document = TextColor(Color.Transparent);

        var states = ExtGStates(document);
        Assert.NotNull(states);
        Assert.Equal(0, Alpha(states!, "GS0", "ca"), 6);
        Assert.Equal(0, Alpha(states!, "GS0", "CA"), 6);

        var text = PageText(document);
        var gs = text.IndexOf("/GS0 gs", StringComparison.Ordinal);
        Assert.True(gs >= 0);
        Assert.True(text.IndexOf(" Tj", gs, StringComparison.Ordinal) > gs);
    }

    [Fact]
    public void FontColorAlpha_MultipliesWithRunOpacity()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var paragraph = new Paragraph();
        var run = paragraph.Inlines.Add("Faded");
        run.Font.Color = Color.FromArgb(128, 255, 0, 0);
        run.Opacity = 0.5;
        section.Blocks.Add(paragraph);

        var states = ExtGStates(document);
        Assert.NotNull(states);
        Assert.Contains(states!.Keys, key => Math.Abs(Alpha(states!, key, "ca") - (0.5 * 128 / 255.0)) < 1e-6);
    }

    [Fact]
    public void TranslucentBackground_FillPaintsThroughExtGState()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Background = Color.FromArgb(128, 0, 0, 255),
        });
        container.Blocks.Add(Text("Boxed"));

        var states = ExtGStates(document);
        Assert.NotNull(states);
        Assert.Contains(states!.Keys, key => Math.Abs(Alpha(states!, key, "ca") - (128 / 255.0)) < 1e-6);

        var text = PageText(document);
        var gs = text.IndexOf("/GS0 gs", StringComparison.Ordinal);
        Assert.True(gs >= 0);
        Assert.True(text.IndexOf(" re f", gs, StringComparison.Ordinal) > gs);
    }

    [Fact]
    public void TranslucentBorderColor_StrokePaintsThroughExtGState()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container());
        container.Borders.Width = 2;
        container.Borders.Color = Color.FromArgb(64, 255, 0, 0);
        container.Blocks.Add(Text("Bordered"));

        var states = ExtGStates(document);
        Assert.NotNull(states);
        Assert.Contains(states!.Keys, key => Math.Abs(Alpha(states!, key, "CA") - (64 / 255.0)) < 1e-6);
    }

    [Fact]
    public void ShadowColorAlpha_IsNotAppliedTwice()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(10),
            Background = Color.FromRgb(255, 255, 255),
            Shadow = new BoxShadow
            {
                Color = Color.FromArgb(160, 0, 0, 0),
                BlurRadius = Unit.FromPoint(8),
            },
        });
        container.Blocks.Add(Text("Shadowed"));

        var states = ExtGStates(document);
        Assert.NotNull(states);
        Assert.Contains(states!.Keys, key => Math.Abs(Alpha(states!, key, "ca") - (160 / 255.0)) < 1e-6);
        Assert.DoesNotContain(states!.Keys, key => Alpha(states!, key, "ca") < 160 / 255.0);
    }

    [Fact]
    public void OpaqueColors_AreByteIdenticalToUnsetAlpha()
    {
        static byte[] Content(bool explicitAlpha)
        {
            var document = new Document();
            var section = document.Sections.Add();
            var paragraph = new Paragraph();
            paragraph.Inlines.Add("Plain").Font.Color =
                explicitAlpha ? Color.FromArgb(255, 255, 0, 0) : Color.FromRgb(255, 0, 0);
            section.Blocks.Add(paragraph);
            var container = section.Blocks.Add(new Container
            {
                Background = explicitAlpha
                    ? Color.FromArgb(255, 200, 200, 200)
                    : Color.FromRgb(200, 200, 200),
            });
            container.Blocks.Add(Text("Boxed"));

            var reader = BuildTestSupport.Read(document);
            var (page, _) = BuildTestSupport.PageLeaves(reader)[0];
            return BuildTestSupport.Content(reader, page);
        }

        Assert.Equal(Content(explicitAlpha: false), Content(explicitAlpha: true));
        Assert.DoesNotContain(" gs\n", Encoding.ASCII.GetString(Content(explicitAlpha: true)), StringComparison.Ordinal);
    }


    private static string DirectContent(ContentElement element)
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(element);
        return Encoding.ASCII.GetString(
            ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0));
    }

    [Fact]
    public void TextContentAlpha_PaintsThroughScopedExtGState()
    {
        var content = DirectContent(new TextContent("Faded", Unit.FromPoint(10), Unit.FromPoint(20))
        {
            Color = Color.FromArgb(128, 255, 0, 0),
        });

        var gs = content.IndexOf("q\n/GS0 gs", StringComparison.Ordinal);
        Assert.True(gs >= 0);
        var show = content.IndexOf(" Tj", gs, StringComparison.Ordinal);
        Assert.True(show > gs);
        Assert.True(content.IndexOf("Q\n", show, StringComparison.Ordinal) > show);
    }

    [Fact]
    public void OpaqueTextContent_EmitsNoExtGState()
    {
        var content = DirectContent(new TextContent("Solid", Unit.FromPoint(10), Unit.FromPoint(20))
        {
            Color = Color.FromRgb(255, 0, 0),
        });

        Assert.DoesNotContain(" gs", content, StringComparison.Ordinal);
    }

    [Fact]
    public void PathContentFillAlpha_PaintsThroughScopedExtGState()
    {
        var path = new PathContent { Fill = true, FillColor = Color.FromArgb(64, 0, 0, 255) };
        path.MoveTo(0, 0);
        path.LineTo(10, 0);
        path.LineTo(10, 10);
        path.Close();

        var content = DirectContent(path);
        var gs = content.IndexOf("q\n/GS0 gs", StringComparison.Ordinal);
        Assert.True(gs >= 0);
        Assert.True(content.IndexOf("Q\n", gs, StringComparison.Ordinal) > gs);
    }

    [Fact]
    public void PathContentDifferingFillAndStrokeAlpha_Throws()
    {
        var path = new PathContent
        {
            Fill = true,
            Stroke = true,
            FillColor = Color.FromArgb(64, 0, 0, 255),
            StrokeColor = Color.FromArgb(128, 255, 0, 0),
        };
        path.MoveTo(0, 0);
        path.LineTo(10, 0);
        path.Close();

        Assert.Throws<NotSupportedException>(() => DirectContent(path));
    }

    [Fact]
    public void PathContentMatchingFillAndStrokeAlpha_PaintsThroughOneExtGState()
    {
        var path = new PathContent
        {
            Fill = true,
            Stroke = true,
            FillColor = Color.FromArgb(64, 0, 0, 255),
            StrokeColor = Color.FromArgb(64, 255, 0, 0),
        };
        path.MoveTo(0, 0);
        path.LineTo(10, 0);
        path.Close();

        Assert.Contains("/GS0 gs", DirectContent(path), StringComparison.Ordinal);
    }
}
