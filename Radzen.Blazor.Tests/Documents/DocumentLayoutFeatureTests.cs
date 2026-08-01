#nullable enable
using System.Collections.Generic;
using System.Linq;
using System;
using Radzen.Documents.Codes;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents;
using Xunit;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;

public class DocumentLayoutFeatureTests
{
    private const string Url = "https://www.radzen.com/";

    private static (Document Builder, Section Section) Author(double width = 400, double height = 300)
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(width), Unit.FromPoint(height));
        section.Margins.SetAll(Unit.FromPoint(40));
        return (document, section);
    }

    [Fact]
    public void LaidOutPage_CarriesLinkAndAnchorGeometry()
    {
        var (document, section) = Author();
        var paragraph = section.Blocks.AddParagraph();
        paragraph.Inlines.Add("Radzen").Link = Url;
        paragraph.Inlines.Add(" target").Anchor = "here";

        var page = Assert.Single(DocumentLayouter.Layout(document).Pages);

        var link = Assert.Single(page.Links);
        Assert.Equal(Url, link.Uri);
        Assert.Null(link.Anchor);
        Assert.True(link.Right > link.Left && link.Bottom > link.Top);
        Assert.Equal("here", Assert.Single(page.Anchors).Name);
    }

    [Fact]
    public void DuplicateAnchorNames_AcrossPagesFailLayout()
    {
        var (document, section) = Author();
        section.Blocks.AddParagraph().Inlines.Add("first").Anchor = "duplicate";
        section.Blocks.AddPageBreak();
        section.Blocks.AddParagraph().Inlines.Add("second").Anchor = "duplicate";

        var error = Assert.Throws<InvalidOperationException>(() => DocumentLayouter.Layout(document));

        Assert.Contains("duplicate", error.Message, StringComparison.Ordinal);
        Assert.Contains("unique", error.Message, StringComparison.Ordinal);
    }

    private static Document NumberedFooter()
    {
        var (document, section) = Author();
        var footer = section.Footer.Blocks.AddParagraph();
        footer.Inlines.Add("Page ");
        footer.Inlines.Add(new PageNumberField());
        footer.Inlines.Add(" of ");
        footer.Inlines.Add(new PageCountField());
        section.Blocks.AddParagraph("one");
        section.Blocks.AddPageBreak();
        section.Blocks.AddParagraph("two");
        section.Blocks.AddPageBreak();
        section.Blocks.AddParagraph("three");
        return document;
    }

    private static string LineText(LaidOutLine line)
        => string.Concat(line.Line.Fragments.Select(fragment => fragment.Text));

    [Fact]
    public void LaidOutFooter_ResolvesPageFieldsPerPage()
    {
        var pages = DocumentLayouter.Layout(NumberedFooter()).Pages;

        Assert.Equal(3, pages.Length);
        for (var i = 0; i < pages.Length; i++)
        {
            Assert.Equal(
                $"Page{i + 1}of3",
                string.Concat(pages[i].FooterLayer.Lines.Select(LineText)));
        }
    }

    [Fact]
    public void LaidOutFooterLayers_AreNotSharedBetweenPages()
    {
        var pages = DocumentLayouter.Layout(NumberedFooter()).Pages;

        Assert.NotSame(pages[0].FooterLayer, pages[1].FooterLayer);
        Assert.NotSame(pages[1].FooterLayer, pages[2].FooterLayer);
    }

    [Fact]
    public void LaidOutBarcode_CarriesCaptionLineGeometry()
    {
        var (document, section) = Author();
        BuildTestSupport.RegisterLatin(document);
        var barcode = section.Blocks.AddBarcode(
            BarcodeType.Code128, "RADZEN-1234", Unit.FromPoint(200), Unit.FromPoint(40), showText: true);
        barcode.Font.Family = BuildTestSupport.Latin;

        var code = Assert.Single(Assert.Single(DocumentLayouter.Layout(document).Pages).Body.CodeSymbols);
        var caption = Assert.Single(code.Caption!.Value);

        Assert.Equal(barcode.Height.Point, caption.Y, 6);
        Assert.Equal("RADZEN-1234", string.Concat(caption.Line.Fragments.Select(fragment => fragment.Text)));
    }

    [Fact]
    public void CapturedGlyphSpans_CarryTheResolvedFace()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        var paragraph = section.Blocks.AddParagraph();
        var run = paragraph.Inlines.Add("Radzen");
        run.Font.Family = BuildTestSupport.Latin;

        var line = Assert.Single(Assert.Single(DocumentLayouter.Layout(document).Pages).Body.Lines);

        var resolved = document.Fonts.ResolveFace(run.Font);

        Assert.All(
            line.Line.Fragments,
            fragment => Assert.All(
                fragment.GlyphRun.Spans,
                span => Assert.Same(resolved, span.Face.Sfnt)));
    }

    [Fact]
    public void FontCollection_EnumeratesRegisteredFacesWithSourceBytes()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);

        var face = Assert.Single(document.Fonts.RegisteredFaces());

        Assert.Equal(BuildTestSupport.Latin, face.Family);
        Assert.False(face.Source.Memory.IsEmpty);
        Assert.Same(face.Face, document.Fonts.ResolveFace(new Font { Family = BuildTestSupport.Latin }));
    }

    [Fact]
    public void AnchorsStable_DetectsMovedAndMissingEntries()
    {
        var before = new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 1, ["b"] = 2 };
        var moved = new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 1, ["b"] = 3 };
        var missing = new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 1 };
        string[] anchors = ["a", "b"];

        Assert.True(DocumentLayouter.AnchorsStable(before, before, anchors));
        Assert.False(DocumentLayouter.AnchorsStable(before, moved, anchors));
        Assert.False(DocumentLayouter.AnchorsStable(before, missing, anchors));
        Assert.True(DocumentLayouter.AnchorsStable(before, missing, ["a"]));
    }

    [Fact]
    public void TableOfContents_SpanningPageBoundary_SettlesOnStablePageNumbers()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var front = document.Sections.Add();
        front.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(150));
        front.Margins.SetAll(Unit.FromPoint(20));
        var toc = front.Blocks.AddTableOfContents();
        toc.Font.Family = BuildTestSupport.Latin;
        toc.Font.Size = 12;

        const int Chapters = 12;
        for (var i = 0; i < Chapters; i++)
        {
            toc.AddEntry($"Chapter {(char)('A' + i)}", $"ch{i}");
        }

        for (var i = 0; i < Chapters; i++)
        {
            var chapter = document.Sections.Add();
            chapter.PageSize = front.PageSize;
            chapter.Margins.SetAll(Unit.FromPoint(20));
            var paragraph = chapter.Blocks.AddParagraph();
            var run = paragraph.Inlines.Add($"Chapter {(char)('A' + i)} body");
            run.Font.Family = BuildTestSupport.Latin;
            run.Anchor = $"ch{i}";
        }

        var pages = DocumentLayouter.Layout(document).Pages;
        var tocPages = pages.Length - Chapters;
        Assert.True(tocPages > 1, $"the table of contents must span more than one page, spanned {tocPages}");

        var numbers = pages
            .Take(tocPages)
            .SelectMany(page => page.Body.Lines)
            .Select(line => System.Text.RegularExpressions.Regex.Match(LineText(line), @"\d+").Value)
            .ToArray();

        Assert.Equal(Chapters, numbers.Length);
        for (var i = 0; i < Chapters; i++)
        {
            Assert.Equal(
                (tocPages + i + 1).ToString(System.Globalization.CultureInfo.InvariantCulture),
                numbers[i]);
        }
    }
}
