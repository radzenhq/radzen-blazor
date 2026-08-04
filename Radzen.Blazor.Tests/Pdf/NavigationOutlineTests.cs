#nullable enable
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class NavigationOutlineTests
{
    private static Document TwoSectionDocument()
    {
        var document = new Document();
        foreach (var (anchor, text) in new[] { ("intro", "Introduction body"), ("details", "Details body") })
        {
            var section = document.Sections.Add();
            section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(300));
            section.Margins.SetAll(Unit.FromPoint(40));
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(text).Anchor = anchor;
            section.Blocks.Add(paragraph);
        }

        return document;
    }

    private static Document PlainDocument()
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(300));
        section.Margins.SetAll(Unit.FromPoint(40));
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("Plain content");
        section.Blocks.Add(paragraph);
        return document;
    }

    private static string[] PageReferences(string emission, int count)
    {
        var catalog = Line(emission, "/Type /Catalog");
        var pages = IndirectObject(emission, Shaped("catalog", @"/Pages (\d+) 0 R", catalog).Groups[1].Value);
        return References("page tree", "Kids", count, pages);
    }

    private static string OutlineRoot(string emission)
        => IndirectObject(
            emission,
            Shaped("catalog", @"/Outlines (\d+) 0 R", Line(emission, "/Type /Catalog")).Groups[1].Value);

    private static string NamedDestinations(string emission)
        => IndirectObject(
            emission,
            Shaped("catalog", @"/Names << /Dests (\d+) 0 R", Line(emission, "/Type /Catalog")).Groups[1].Value);

    private static double Coordinate(Match destination, int group)
        => double.Parse(destination.Groups[group].Value, CultureInfo.InvariantCulture);

    [Fact]
    public void Outline_EmitsCatalogTree_WithTitlesAndNesting()
    {
        var document = TwoSectionDocument();
        var child = new OutlineItem("Details", OutlineTarget.ToAnchor("details"));
        var root = new OutlineItem("Introduction", OutlineTarget.ToAnchor("intro"));
        root.Children.Add(child);
        var rendered = new DocumentRenderer().Render(document);
        rendered.Outline.Add(root);

        var emission = Emit(rendered);
        var rootNumber = Shaped("catalog", @"/Outlines (\d+) 0 R", Line(emission, "/Type /Catalog")).Groups[1].Value;
        var outlines = IndirectObject(emission, rootNumber);

        Carries("outline root", "/Type /Outlines", outlines);
        Carries("outline root", "/Count 2", outlines);

        var firstNumber = Shaped("outline root", @"/First (\d+) 0 R", outlines).Groups[1].Value;
        var lastNumber = Shaped("outline root", @"/Last (\d+) 0 R", outlines).Groups[1].Value;
        var first = IndirectObject(emission, firstNumber);

        Carries("outline first item", "/Title (Introduction)", first);
        Carries("outline last item", "/Title (Introduction)", IndirectObject(emission, lastNumber));
        Carries("outline first item", $"/Parent {rootNumber} 0 R", first);
        Carries("outline first item", "/Dest (intro)", first);
        Carries("outline first item", "/Count 1", first);

        var nestedNumber = Shaped("outline first item", @"/First (\d+) 0 R", first).Groups[1].Value;
        var nested = IndirectObject(emission, nestedNumber);

        Carries("nested outline item", "/Title (Details)", nested);
        Carries("nested outline item", $"/Parent {firstNumber} 0 R", nested);
        Carries("nested outline item", "/Dest (details)", nested);
        Carries("outline first item", $"/Last {nestedNumber} 0 R", first);
        Lacks("nested outline item", "/Next", nested);
        Lacks("nested outline item", "/Prev", nested);
    }

    [Fact]
    public void OutlineSiblings_LinkPrevAndNext()
    {
        var document = TwoSectionDocument();
        var rendered = new DocumentRenderer().Render(document);
        rendered.Outline.Add(new OutlineItem("Introduction", OutlineTarget.ToAnchor("intro")));
        rendered.Outline.Add(new OutlineItem("Details", OutlineTarget.ToAnchor("details")));

        var emission = Emit(rendered);
        var outlines = OutlineRoot(emission);

        Carries("outline root", "/Count 2", outlines);

        var firstNumber = Shaped("outline root", @"/First (\d+) 0 R", outlines).Groups[1].Value;
        var lastNumber = Shaped("outline root", @"/Last (\d+) 0 R", outlines).Groups[1].Value;
        var first = IndirectObject(emission, firstNumber);
        var last = IndirectObject(emission, lastNumber);

        Carries("outline first item", "/Title (Introduction)", first);
        Carries("outline last item", "/Title (Details)", last);
        Carries("outline first item", $"/Next {lastNumber} 0 R", first);
        Carries("outline last item", $"/Prev {firstNumber} 0 R", last);
        Lacks("outline first item", "/Prev", first);
        Lacks("outline last item", "/Next", last);
    }

    [Fact]
    public void OutlinePageTarget_EmitsExplicitDestination()
    {
        var document = TwoSectionDocument();
        var rendered = new DocumentRenderer().Render(document);
        rendered.Outline.Add(new OutlineItem("Second page", OutlineTarget.ToPage(1)));

        var emission = Emit(rendered);
        var pages = PageReferences(emission, 2);
        var outlines = OutlineRoot(emission);
        var item = IndirectObject(emission, Shaped("outline root", @"/First (\d+) 0 R", outlines).Groups[1].Value);

        var destination = Shaped(
            "outline item /Dest",
            $@"/Dest \[{pages[1]} 0 R /XYZ ([^ \]]+) ([^ \]]+) ([^ \]]+)\]",
            item);

        Assert.Equal(300, Coordinate(destination, 2), 0.5);
    }

    [Fact]
    public void Anchors_EmitNamedDestinations_OnTheRightPages()
    {
        var emission = Emit(new DocumentRenderer().Render(TwoSectionDocument()));
        var pages = PageReferences(emission, 2);
        var dests = NamedDestinations(emission);

        var entries = Regex.Matches(dests, @"\([^)]*\) \[");
        Assert.True(
            entries.Count == 2,
            $"Expected 2 named destinations, found {entries.Count}.\n/Dests node:\n{Excerpt(dests)}");

        foreach (var (name, page) in new[] { ("intro", pages[0]), ("details", pages[1]) })
        {
            var destination = Shaped(
                $"named destination ({name})",
                $@"\({name}\) \[{page} 0 R /XYZ ([^ \]]+) ([^ \]]+) ([^ \]]+)\]",
                dests);

            Assert.InRange(Coordinate(destination, 2), 1, 300);
        }
    }

    [Fact]
    public void RunLinkToAnchor_EmitsGoToLinkAnnotation()
    {
        var document = TwoSectionDocument();
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("jump to details").LinkToAnchor = "details";
        document.Sections[0].Blocks.Add(paragraph);

        var emission = Emit(new DocumentRenderer().Render(document));
        var pages = PageReferences(emission, 2);
        var annots = References("first page", "Annots", 1, IndirectObject(emission, pages[0]));
        var annotation = IndirectObject(emission, annots[0]);

        Carries("link annotation", "/Subtype /Link", annotation);
        Carries("link annotation", "/A << /S /GoTo /D (details) >>", annotation);

        Shaped(
            "named destination (details)",
            $@"\(details\) \[{pages[1]} 0 R ",
            NamedDestinations(emission));
    }

    [Fact]
    public void GeneratedPageRotate_EmitsRotate90()
    {
        var document = new DocumentRenderer().Render(PlainDocument());
        document.Pages[0].Rotate = 90;

        Assert.Equal(90, NumberIn(Line(Emit(document), "/Type /Page "), "Rotate"));
    }

    [Fact]
    public void PageRotate_RejectsInvalidAngle()
    {
        var document = new DocumentRenderer().Render(PlainDocument());
        Assert.Throws<ArgumentOutOfRangeException>(() => document.Pages[0].Rotate = 45);
    }

    [Fact]
    public void PlainDocument_EmitsNoNavigationKeys_AndStaysDeterministic()
    {
        var emission = Emit(new DocumentRenderer().Render(PlainDocument()));
        Assert.Equal(emission, Emit(new DocumentRenderer().Render(PlainDocument())));

        var catalog = Line(emission, "/Type /Catalog");
        Lacks("catalog", "/Outlines", catalog);
        Lacks("catalog", "/Dests", catalog);
        Lacks("catalog", "/Names", catalog);

        var page = Line(emission, "/Type /Page ");
        Lacks("page", "/Rotate", page);
        Lacks("page", "/Annots", page);
    }
}
