#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// TableOfContents block: two-pass page-number resolution, dot leaders, GoTo link
// annotations on the TOC page, single-pass byte identity for documents without a TOC
// and the missing-anchor failure. All assertions run against real DocumentBuilder.Build()
// bytes reloaded through DocumentReader / Document.LoadFromStream.
public class TableOfContentsTests
{
    // Page 1: the TOC. Page 2: "Chapter One" (anchor ch1). Pages 3-4: filler, then a
    // page break puts "Chapter Two" (anchor ch2) on page 4.
    private static DocumentBuilder ChapterDocument()
    {
        var builder = new DocumentBuilder();

        var front = builder.Sections.Add();
        front.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(300));
        front.Margin = Unit.FromPoint(40);
        var toc = front.Blocks.AddTableOfContents();
        toc.AddEntry("Chapter One", "ch1");
        toc.AddEntry("Chapter Two", "ch2", level: 1);

        var one = builder.Sections.Add();
        one.PageSize = front.PageSize;
        one.Margin = Unit.FromPoint(40);
        var heading = new Paragraph();
        heading.Inlines.Add("Chapter One body").Anchor = "ch1";
        one.Blocks.Add(heading);

        var two = builder.Sections.Add();
        two.PageSize = front.PageSize;
        two.Margin = Unit.FromPoint(40);
        two.Blocks.AddParagraph("Filler before the break");
        two.Blocks.AddPageBreak();
        var second = new Paragraph();
        second.Inlines.Add("Chapter Two body").Anchor = "ch2";
        two.Blocks.Add(second);

        return builder;
    }

    private static DocumentBuilder PlainNavigationDocument()
    {
        var builder = new DocumentBuilder();
        foreach (var (anchor, text) in new[] { ("intro", "Introduction body"), ("details", "Details body") })
        {
            var section = builder.Sections.Add();
            section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(300));
            section.Margin = Unit.FromPoint(40);
            var paragraph = new Paragraph();
            paragraph.Inlines.Add(text).Anchor = anchor;
            section.Blocks.Add(paragraph);
        }

        var link = new Paragraph();
        link.Inlines.Add("jump").LinkToAnchor = "details";
        builder.Sections[0].Blocks.Add(link);
        return builder;
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
        var content = Encoding.Latin1.GetString(
            ContentTestHelpers.PageContent(BuildTestSupport.Read(ChapterDocument()), 0));

        // Both entry lines start at the left content edge (x = 40) plus the level indent (12pt).
        Assert.Contains("40 ", content, StringComparison.Ordinal);
        Assert.Contains("52 ", content, StringComparison.Ordinal);
    }

    [Fact]
    public void DocumentWithoutToc_StaysSinglePass_AndByteIdentical()
    {
        var bytes = PlainNavigationDocument().ToArray();
        Assert.Equal(bytes, PlainNavigationDocument().ToArray());
    }

    // Exercises the two-pass pagination identity where it is least trivial: many entries
    // spilling the TOC itself across pages, and an entry whose text reaches into the
    // page-number column. Wrong numbers here would mean the passes broke lines differently.
    [Fact]
    public void Toc_ManyAndOverlongEntries_ResolveCorrectPageNumbers()
    {
        var builder = new DocumentBuilder();
        var front = builder.Sections.Add();
        front.PageSize = new PageSize(Unit.FromPoint(300), Unit.FromPoint(160));
        front.Margin = Unit.FromPoint(40);
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
            var section = builder.Sections.Add();
            section.PageSize = front.PageSize;
            section.Margin = Unit.FromPoint(40);
            var heading = new Paragraph();
            heading.Inlines.Add($"Body {i}").Anchor = $"ch{i}";
            section.Blocks.Add(heading);
        }

        var document = BuildTestSupport.Reload(builder);
        var tocPageCount = document.Pages.Count - 12;
        Assert.True(tocPageCount >= 2, "the TOC itself should spill across pages");

        var text = string.Concat(
            Enumerable.Range(0, tocPageCount).Select(p => document.Pages[p].ExtractText() + "\n"));
        Assert.Matches(new Regex(@"column\s*" + (tocPageCount + 1) + @"\b"), text);
        for (var i = 1; i < 12; i++)
        {
            Assert.Matches(new Regex($@"Chapter {i}\s*\.{{3,}}\s*{tocPageCount + 1 + i}\b"), text);
        }
    }

    [Fact]
    public void Toc_MissingAnchor_Throws()
    {
        var builder = ChapterDocument();
        ((TableOfContents)builder.Sections[0].Blocks[0]).AddEntry("Ghost", "nowhere");

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
        Assert.Contains("'nowhere'", exception.Message, StringComparison.Ordinal);
    }
}
