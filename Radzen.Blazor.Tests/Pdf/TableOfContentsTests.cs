#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

namespace Radzen.Blazor.Pdf.Tests;

public class TableOfContentsTests
{
    private static Document ChapterDocument()
    {
        var document = new Document();

        var front = document.Sections.Add();
        front.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(300));
        front.Margins.SetAll(Unit.FromPoint(40));
        var toc = front.Blocks.AddTableOfContents();
        toc.AddEntry("Chapter One", "ch1");
        toc.AddEntry("Chapter Two", "ch2", level: 1);

        var one = document.Sections.Add();
        one.PageSize = front.PageSize;
        one.Margins.SetAll(Unit.FromPoint(40));
        var heading = new Paragraph();
        heading.Inlines.Add("Chapter One body").Anchor = "ch1";
        one.Blocks.Add(heading);

        var two = document.Sections.Add();
        two.PageSize = front.PageSize;
        two.Margins.SetAll(Unit.FromPoint(40));
        two.Blocks.AddParagraph("Filler before the break");
        two.Blocks.AddPageBreak();
        var second = new Paragraph();
        second.Inlines.Add("Chapter Two body").Anchor = "ch2";
        two.Blocks.Add(second);

        return document;
    }

    private static Document PlainNavigationDocument()
    {
        var pdf = new Document();
        foreach (var (anchor, text) in new[] { ("intro", "Introduction body"), ("details", "Details body") })
        {
            var section = pdf.Sections.Add();
            section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(300));
            section.Margins.SetAll(Unit.FromPoint(40));
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(text).Anchor = anchor;
            section.Blocks.Add(paragraph);
        }

        var link = new Paragraph();
        link.Inlines.Add("jump").LinkToAnchor = "details";
        pdf.Sections[0].Blocks.Add(link);
        return pdf;
    }

    private static Dictionary<string, ArrayObject> NamedDestinations(DocumentReader reader)
    {
        var catalog = ContentTestHelpers.Catalog(reader);
        var names = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Names"]));
        var dests = Assert.IsType<DictionaryObject>(reader.Resolve(names["Dests"]));
        var entries = Assert.IsType<ArrayObject>(reader.Resolve(dests["Names"]));
        var result = new Dictionary<string, ArrayObject>(StringComparer.Ordinal);
        for (var i = 0; i + 1 < entries.Count; i += 2)
        {
            var name = Assert.IsType<StringObject>(reader.Resolve(entries[i]));
            result[name.Value] = Assert.IsType<ArrayObject>(reader.Resolve(entries[i + 1]));
        }

        return result;
    }

    [Fact]
    public void Toc_RendersEntryLines_WithLeaderAndResolvedPageNumbers()
    {
        var document = BuildTestSupport.Reload(ChapterDocument());
        Assert.Equal(4, document.Pages.Count);

        var text = document.Pages[0].ExtractText();
        Assert.Matches(new Regex(@"Chapter One\s*\.{3,}\s*2"), text);
        Assert.Matches(new Regex(@"Chapter Two\s*\.{3,}\s*4"), text);
        Assert.DoesNotContain("0000", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Toc_EmitsGoToLinkAnnotations_ToEveryEntryAnchor()
    {
        var reader = BuildTestSupport.Read(ChapterDocument());
        var page = ContentTestHelpers.Kid(reader, 0);
        var annots = Assert.IsType<ArrayObject>(reader.Resolve(page["Annots"]));

        var destinations = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in annots)
        {
            var annot = Assert.IsType<DictionaryObject>(reader.Resolve(entry));
            Assert.Equal("Link", Assert.IsType<NameObject>(reader.Resolve(annot["Subtype"])).Value);
            var action = Assert.IsType<DictionaryObject>(reader.Resolve(annot["A"]));
            Assert.Equal("GoTo", Assert.IsType<NameObject>(reader.Resolve(action["S"])).Value);
            destinations.Add(Assert.IsType<StringObject>(reader.Resolve(action["D"])).Value);
        }

        Assert.Equal(new HashSet<string>(new[] { "ch1", "ch2" }, StringComparer.Ordinal), destinations);

        var dests = NamedDestinations(reader);
        Assert.Same(ContentTestHelpers.Kid(reader, 1), Assert.IsType<DictionaryObject>(reader.Resolve(dests["ch1"][0])));
        Assert.Same(ContentTestHelpers.Kid(reader, 3), Assert.IsType<DictionaryObject>(reader.Resolve(dests["ch2"][0])));
    }

    [Fact]
    public void Toc_IndentsEntries_ByLevel()
    {
        var content = ContentTestHelpers.PageContent(BuildTestSupport.Read(ChapterDocument()), 0);
        var starts = ContentStreamTokenizer.Parse(content)
            .FindAll(operation => operation.Operator == "Td")
            .ConvertAll(operation => operation.Num(0));

        Assert.Contains(40d, starts);
        Assert.Contains(52d, starts);
    }

    [Fact]
    public void DocumentWithoutToc_StaysSinglePass_AndByteIdentical()
    {
        var bytes = new DocumentRenderer().ToArray(PlainNavigationDocument());
        Assert.Equal(bytes, new DocumentRenderer().ToArray(PlainNavigationDocument()));
    }

    [Fact]
    public void Toc_ManyAndOverlongEntries_ResolveCorrectPageNumbers()
    {
        var document = new Document();
        var front = document.Sections.Add();
        front.PageSize = new PageSize(Unit.FromPoint(300), Unit.FromPoint(160));
        front.Margins.SetAll(Unit.FromPoint(40));
        var toc = front.Blocks.AddTableOfContents();
        toc.AddEntry(
            "An exceedingly long heading that wraps across lines and runs into the page number column",
            "ch0");
        for (var i = 1; i < 12; i++)
        {
            toc.AddEntry($"Chapter {i}", $"ch{i}");
        }

        for (var i = 0; i < 12; i++)
        {
            var section = document.Sections.Add();
            section.PageSize = front.PageSize;
            section.Margins.SetAll(Unit.FromPoint(40));
            var heading = new Paragraph();
            heading.Inlines.Add($"Body {i}").Anchor = $"ch{i}";
            section.Blocks.Add(heading);
        }

        var pdf = BuildTestSupport.Reload(document);
        var tocPageCount = pdf.Pages.Count - 12;
        Assert.True(tocPageCount >= 2, "the TOC itself should spill across pages");

        var text = string.Concat(
            Enumerable.Range(0, tocPageCount).Select(p => pdf.Pages[p].ExtractText() + "\n"));
        Assert.Matches(new Regex(@"column\s*" + (tocPageCount + 1) + @"\b"), text);
        for (var i = 1; i < 12; i++)
        {
            Assert.Matches(new Regex($@"Chapter {i}\s*\.{{3,}}\s*{tocPageCount + 1 + i}\b"), text);
        }
    }

    [Fact]
    public void Toc_PageNumbers_ShareOneRightAlignedColumn()
    {
        var document = new Document();
        var front = document.Sections.Add();
        front.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(300));
        front.Margins.SetAll(Unit.FromPoint(40));
        var toc = front.Blocks.AddTableOfContents();
        foreach (var (text, anchor) in new[]
        {
            ("Chapter One", "ch1"),
            ("A Considerably Longer Chapter Title Here", "ch2"),
            ("Mid", "ch3"),
        })
        {
            toc.AddEntry(text, anchor);
            var section = document.Sections.Add();
            section.PageSize = front.PageSize;
            section.Margins.SetAll(Unit.FromPoint(40));
            var paragraph = new Paragraph();
            paragraph.Inlines.Add("Body " + anchor).Anchor = anchor;
            section.Blocks.Add(paragraph);
        }

        var content = CascadeTestSupport.FirstPageContent(document);

        var numberX = Regex.Matches(content, @"(-?[\d.]+) (?:-?[\d.]+) Td\s*\((\d+)\) Tj")
            .Select(m => double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture))
            .ToList();

        Assert.Equal(3, numberX.Count);
        Assert.All(numberX, x => Assert.Equal(numberX[0], x, 3));
    }

    [Fact]
    public void Toc_WithoutExplicitFont_InheritsNormalStyleFont()
    {
        var document = ChapterDocument();
        document.Styles.Normal.Font.Size = 14;

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Contains(14.0, sizes);
        Assert.DoesNotContain(10.0, sizes);
    }

    [Fact]
    public void Toc_ExplicitFont_WinsOverNormalStyleFont()
    {
        var document = ChapterDocument();
        document.Styles.Normal.Font.Size = 14;
        ((TableOfContents)document.Sections[0].Blocks[0]).Font.Size = 8;

        var sizes = CascadeTestSupport.TfSizes(CascadeTestSupport.FirstPageContent(document));

        Assert.Contains(8.0, sizes);
        Assert.DoesNotContain(14.0, sizes);
    }

    [Fact]
    public void Toc_MissingAnchor_Throws()
    {
        var document = ChapterDocument();
        ((TableOfContents)document.Sections[0].Blocks[0]).AddEntry("Ghost", "nowhere");

        var exception = Assert.Throws<InvalidOperationException>(() => new DocumentRenderer().Render(document));
        Assert.Contains("'nowhere'", exception.Message, StringComparison.Ordinal);
    }

    private static Document WideEntryDocument(int pagesBeforeTheAnchor)
    {
        var document = new Document();
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(120), Unit.FromPoint(60));
        section.Margins.SetAll(Unit.FromPoint(5));
        section.Blocks.AddTableOfContents().AddEntry(new string('W', 24), "target");

        for (var page = 0; page < pagesBeforeTheAnchor; page++)
        {
            section.Blocks.AddPageBreak();
        }

        var heading = new Paragraph();
        heading.Inlines.Add("H").Anchor = "target";
        section.Blocks.Add(heading);

        return document;
    }

    [Theory]
    [InlineData(8)]
    [InlineData(98)]
    [InlineData(998)]
    [InlineData(9998)]
    public void TocEntryLineCount_IsIndependentOfTheResolvedPageNumberWidth(int pagesBeforeTheAnchor)
    {
        var baseline = Radzen.Documents.Layout.DocumentLayouter.Layout(WideEntryDocument(8));
        var wide = Radzen.Documents.Layout.DocumentLayouter.Layout(WideEntryDocument(pagesBeforeTheAnchor));

        Assert.Equal(baseline.Pages[0].Body.Lines.Length, wide.Pages[0].Body.Lines.Length);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(998)]
    [InlineData(10998)]
    public void TocPageNumbers_SettleWithinThreePassesAtEveryDigitWidth(int pagesBeforeTheAnchor)
    {
        var laidOut = Radzen.Documents.Layout.DocumentLayouter.Layout(WideEntryDocument(pagesBeforeTheAnchor));
        var number = (pagesBeforeTheAnchor + 1).ToString(CultureInfo.InvariantCulture);

        Assert.Equal(pagesBeforeTheAnchor + 1, laidOut.Pages.Length);
        Assert.Contains(
            laidOut.Pages[0].Body.Lines,
            line => string.Concat(line.Line.Fragments.Select(fragment => fragment.Text)).EndsWith(number, StringComparison.Ordinal));
    }
}
