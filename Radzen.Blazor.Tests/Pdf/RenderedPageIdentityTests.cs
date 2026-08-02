#nullable enable
using System;
using System.Collections.Generic;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class RenderedPageIdentityTests
{
    private static Document ThreePages(bool anchors)
    {
        var document = new Document { Language = "en" };
        document.Info.Title = "Page identity";
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        for (var page = 0; page < 3; page++)
        {
            var paragraph = BuildTestSupport.AddText(section, $"Page {page} body", BuildTestSupport.Latin);
            if (anchors)
            {
                paragraph.Inlines[0].Anchor = $"anchor{page}";
            }

            if (page < 2)
            {
                section.Blocks.Add(new PageBreak());
            }
        }

        return document;
    }

    private static PortableDocument Tagged(bool anchors)
        => new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 }.Render(ThreePages(anchors));

    private static List<int> StructurePageOrder(byte[] bytes)
    {
        var reader = DocumentReader.Parse(bytes);
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        var root = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["StructTreeRoot"]));
        var pages = PageNumbers(reader, catalog);

        var order = new List<int>();
        Walk(reader, root, pages, order);
        return order;
    }

    private static void Walk(DocumentReader reader, DictionaryObject node, Dictionary<int, int> pages, List<int> order)
    {
        if (node.TryGetValue("Pg", out var page) && page is ReferenceObject reference
            && pages.TryGetValue(reference.ObjectNumber, out var index)
            && (order.Count == 0 || order[^1] != index))
        {
            order.Add(index);
        }

        if (!node.TryGetValue("K", out var kids))
        {
            return;
        }

        foreach (var kid in reader.Resolve(kids!) is ArrayObject array ? array : [kids!])
        {
            if (reader.AsDictionary(kid) is { } child)
            {
                Walk(reader, child, pages, order);
            }
        }
    }

    private static Dictionary<int, int> PageNumbers(DocumentReader reader, DictionaryObject catalog)
    {
        var tree = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Pages"]));
        var kids = Assert.IsType<ArrayObject>(reader.Resolve(tree["Kids"]));
        var pages = new Dictionary<int, int>();
        for (var index = 0; index < kids.Count; index++)
        {
            pages[Assert.IsType<ReferenceObject>(kids[index]).ObjectNumber] = index;
        }

        return pages;
    }

    private static int DestinationPage(byte[] bytes, string name)
    {
        var reader = DocumentReader.Parse(bytes);
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        var names = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["Names"]));
        var destinations = Assert.IsType<DictionaryObject>(reader.Resolve(names["Dests"]));
        var target = FindDestination(reader, destinations, name)
            ?? throw new InvalidOperationException($"No destination '{name}'.");

        return PageNumbers(reader, catalog)[Assert.IsType<ReferenceObject>(target[0]).ObjectNumber];
    }

    private static ArrayObject? FindDestination(DocumentReader reader, DictionaryObject node, string name)
    {
        if (reader.GetArray(node, "Names") is { } entries)
        {
            for (var index = 0; index + 1 < entries.Count; index += 2)
            {
                if (reader.AsString(entries[index]) == name)
                {
                    return reader.AsArray(entries[index + 1]);
                }
            }
        }

        if (reader.GetArray(node, "Kids") is { } kids)
        {
            foreach (var kid in kids)
            {
                if (reader.AsDictionary(kid) is { } child && FindDestination(reader, child, name) is { } found)
                {
                    return found;
                }
            }
        }

        return null;
    }

    [Fact]
    public void RemovingATaggedPage_ThrowsNamingTheStructureTree()
    {
        var rendered = Tagged(anchors: false);
        rendered.Pages.RemoveAt(1);

        var error = Assert.Throws<InvalidOperationException>(() => rendered.ToArray());

        Assert.Contains("structure tree", error.Message, StringComparison.Ordinal);
        Assert.Contains("no longer in Pages", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RemovingAnAnchoredPage_ThrowsNamingTheDestination()
    {
        var rendered = new DocumentRenderer().Render(ThreePages(anchors: true));
        var plan = rendered.Output!;
        rendered.Output = new Radzen.Documents.Pdf.Output.DocumentOutput(
            plan.Pages, null, plan.Anchors, plan.UnmappedRoles);
        rendered.Pages.RemoveAt(1);

        var error = Assert.Throws<InvalidOperationException>(() => rendered.ToArray());

        Assert.Contains("named destination 'anchor1'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void MovingATaggedPage_MovesItsStructureMarks()
    {
        var rendered = Tagged(anchors: false);
        Assert.Equal([0, 1, 2], StructurePageOrder(rendered.ToArray()));

        rendered.Pages.Move(0, 2);

        Assert.Equal([2, 0, 1], StructurePageOrder(rendered.ToArray()));
    }

    [Fact]
    public void MovingAnAnchoredPage_MovesItsDestination()
    {
        var rendered = new DocumentRenderer().Render(ThreePages(anchors: true));
        Assert.Equal(0, DestinationPage(rendered.ToArray(), "anchor0"));

        rendered.Pages.Move(0, 2);

        Assert.Equal(2, DestinationPage(rendered.ToArray(), "anchor0"));
        Assert.Equal(0, DestinationPage(rendered.ToArray(), "anchor1"));
    }

    [Fact]
    public void ReorderingAnUntaggedRenderedDocument_WritesThePagesInTheNewOrder()
    {
        var rendered = new DocumentRenderer().Render(ThreePages(anchors: false));
        _ = rendered.ToArray();

        rendered.Pages.Move(2, 0);
        var bytes = rendered.ToArray();

        Assert.Equal(3, DocumentLoadTests.PageCount(DocumentReader.Parse(bytes)));
        Assert.Contains("Page 2 body", rendered.Pages[0].ExtractText(), StringComparison.Ordinal);
        Assert.StartsWith("Page 2 body",
            PortableDocument.LoadFromStream(new System.IO.MemoryStream(bytes)).Pages[0].ExtractText(),
            StringComparison.Ordinal);
    }
}
