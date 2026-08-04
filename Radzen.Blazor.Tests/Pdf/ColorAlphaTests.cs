#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

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

    private static string PageDictionary(Document document)
        => Line(Emit(new DocumentRenderer().Render(document)), "/Type /Page ");

    private static string PageContentObject(string emission)
    {
        var page = Line(emission, "/Type /Page ");
        return IndirectObject(emission, Shaped("page", @"/Contents (\d+) 0 R", page).Groups[1].Value);
    }

    private static double Number(string value) => double.Parse(value, CultureInfo.InvariantCulture);

    private static (double Fill, double Stroke) ExtGState(string page, string key)
    {
        var match = Shaped(
            $"page /ExtGState /{key}",
            $@"/{key} << /Type /ExtGState /ca (-?[\d.]+) /CA (-?[\d.]+)",
            page);
        return (Number(match.Groups[1].Value), Number(match.Groups[2].Value));
    }

    private static List<(double Fill, double Stroke)> ExtGStates(string page)
    {
        var states = new List<(double, double)>();
        foreach (Match match in Regex.Matches(page, @"/Type /ExtGState /ca (-?[\d.]+) /CA (-?[\d.]+)"))
        {
            states.Add((Number(match.Groups[1].Value), Number(match.Groups[2].Value)));
        }

        return states;
    }

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

        var (fill, stroke) = ExtGState(PageDictionary(document), "GS0");
        Assert.Equal(128 / 255.0, fill, 6);
        Assert.Equal(128 / 255.0, stroke, 6);

        var text = PageText(document);
        var gs = text.IndexOf("/GS0 gs", StringComparison.Ordinal);
        Assert.True(gs >= 0);
        Assert.True(text.IndexOf(" Tj", gs, StringComparison.Ordinal) > gs);
    }

    [Fact]
    public void TransparentFontColor_PaintsNothing()
    {
        var document = TextColor(Color.Transparent);

        var (fill, stroke) = ExtGState(PageDictionary(document), "GS0");
        Assert.Equal(0, fill, 6);
        Assert.Equal(0, stroke, 6);

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

        var page = PageDictionary(document);
        var expected = 0.5 * 128 / 255.0;

        Assert.True(
            ExtGStates(page).Exists(state => Math.Abs(state.Fill - expected) < 1e-6),
            $"No /ExtGState carries /ca {expected}.\npage:\n{Excerpt(page)}");
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

        var page = PageDictionary(document);

        Assert.True(
            ExtGStates(page).Exists(state => Math.Abs(state.Fill - (128 / 255.0)) < 1e-6),
            $"No /ExtGState carries /ca {128 / 255.0}.\npage:\n{Excerpt(page)}");

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
        container.Borders.SetAll(width: 2, color: Color.FromArgb(64, 255, 0, 0));
        container.Blocks.Add(Text("Bordered"));

        var page = PageDictionary(document);

        Assert.True(
            ExtGStates(page).Exists(state => Math.Abs(state.Stroke - (64 / 255.0)) < 1e-6),
            $"No /ExtGState carries /CA {64 / 255.0}.\npage:\n{Excerpt(page)}");
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

        var page = PageDictionary(document);
        var states = ExtGStates(page);

        Assert.True(
            states.Exists(state => Math.Abs(state.Fill - (160 / 255.0)) < 1e-6),
            $"No /ExtGState carries /ca {160 / 255.0}.\npage:\n{Excerpt(page)}");
        Assert.True(
            !states.Exists(state => state.Fill < 160 / 255.0),
            $"An /ExtGState carries /ca below {160 / 255.0}.\npage:\n{Excerpt(page)}");
    }

    [Fact]
    public void OpaqueColors_AreByteIdenticalToUnsetAlpha()
    {
        static string Emission(bool explicitAlpha)
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

            return Emit(new DocumentRenderer().Render(document));
        }

        var authored = Emission(explicitAlpha: true);

        Assert.Equal(PageContentObject(Emission(explicitAlpha: false)), PageContentObject(authored));
        Lacks("page", "/ExtGState", Line(authored, "/Type /Page "));
    }

    private static string DirectContent(ContentElement element)
    {
        var document = new PortableDocument();
        document.Pages.Add().Content.Add(element);
        return PageContentObject(Emit(document));
    }

    [Fact]
    public void TextContentAlpha_PaintsThroughScopedExtGState()
    {
        var content = DirectContent(new TextContent("Faded", Unit.FromPoint(10), Unit.FromPoint(20))
        {
            Color = Color.FromArgb(128, 255, 0, 0),
        });

        Shaped("page content", @"q\n/GS0 gs\n[\s\S]*?Tj[\s\S]*?\nQ\n", content);
    }

    [Fact]
    public void OpaqueTextContent_EmitsNoExtGState()
    {
        var content = DirectContent(new TextContent("Solid", Unit.FromPoint(10), Unit.FromPoint(20))
        {
            Color = Color.FromRgb(255, 0, 0),
        });

        Lacks("page content", " gs\n", content);
    }

    [Fact]
    public void PathContentFillAlpha_PaintsThroughScopedExtGState()
    {
        var path = new PathContent { Fill = true, FillColor = Color.FromArgb(64, 0, 0, 255) };
        path.MoveTo(0, 0);
        path.LineTo(10, 0);
        path.LineTo(10, 10);
        path.Close();

        Shaped("page content", @"q\n/GS0 gs\n[\s\S]*?\nQ\n", DirectContent(path));
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

        Carries("page content", "/GS0 gs\n", DirectContent(path));
    }
}
