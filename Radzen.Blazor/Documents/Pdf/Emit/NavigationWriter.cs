using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class NavigationWriter(Document document)
{
    public void WriteDestinations(
        DocumentWriter writer,
        DictionaryObject catalog,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes)
    {
        var sorted = new SortedDictionary<string, GeneratedAnchor>(document.Anchors, StringComparer.Ordinal);
        var names = new ArrayObject();
        foreach (var (name, anchor) in sorted)
        {
            names.Add(new StringObject(name));
            names.Add(DestinationArray(pageNodes[anchor.PageIndex].Reference, anchor.Top));
        }

        var dests = writer.Add(new DictionaryObject { ["Names"] = names });
        if (catalog.TryGetValue("Names", out var existing) && existing is DictionaryObject tree)
        {
            tree["Dests"] = dests;
        }
        else
        {
            catalog["Names"] = new DictionaryObject { ["Dests"] = dests };
        }
    }

    public ReferenceObject WriteOutline(
        DocumentWriter writer,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes)
    {
        var root = new DictionaryObject { ["Type"] = new NameObject("Outlines") };
        var rootRef = writer.Add(root);
        root["Count"] = new NumberObject(WriteOutlineLevel(writer, document.Outline, root, rootRef, pageNodes));
        return rootRef;
    }

    private int WriteOutlineLevel(
        DocumentWriter writer,
        IList<OutlineItem> items,
        DictionaryObject parent,
        ReferenceObject parentRef,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes)
    {
        var nodes = new List<(DictionaryObject Node, ReferenceObject Reference)>(items.Count);
        foreach (var item in items)
        {
            var node = new DictionaryObject
            {
                ["Title"] = new StringObject(item.Title),
                ["Parent"] = parentRef,
            };
            if (item.Target is { } target)
            {
                node["Dest"] = OutlineDestination(target, pageNodes);
            }

            if (item.Color is { } color)
            {
                node["C"] = PdfColorArray.Rgb(color);
            }

            var flags = (item.Italic ? 1 : 0) | (item.Bold ? 2 : 0);
            if (flags != 0)
            {
                node["F"] = new NumberObject(flags);
            }

            nodes.Add((node, writer.Add(node)));
        }

        var levelVisible = 0;
        for (var i = 0; i < items.Count; i++)
        {
            if (i > 0)
            {
                nodes[i].Node["Prev"] = nodes[i - 1].Reference;
            }

            if (i < items.Count - 1)
            {
                nodes[i].Node["Next"] = nodes[i + 1].Reference;
            }

            var visibleDescendants = 0;
            if (items[i].Children.Count > 0)
            {
                visibleDescendants = WriteOutlineLevel(writer, [.. items[i].Children], nodes[i].Node, nodes[i].Reference, pageNodes);
                nodes[i].Node["Count"] = new NumberObject(items[i].Collapsed ? -visibleDescendants : visibleDescendants);
            }

            levelVisible += 1 + (items[i].Collapsed ? 0 : visibleDescendants);
        }

        parent["First"] = nodes[0].Reference;
        parent["Last"] = nodes[^1].Reference;
        return levelVisible;
    }

    private DocumentObject OutlineDestination(
        OutlineTarget target,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes)
    {
        if (target.Anchor is { } anchor)
        {
            if (!document.Anchors.ContainsKey(anchor))
            {
                throw new InvalidOperationException($"Outline target anchor '{anchor}' does not exist; set Run.Anchor on the destination run.");
            }

            return new StringObject(anchor);
        }

        var pageIndex = target.PageIndex ?? 0;
        if (pageIndex >= pageNodes.Count)
        {
            throw new InvalidOperationException($"Outline target page index {pageIndex} is out of range; the document has {pageNodes.Count} pages.");
        }

        var (page, _, reference) = pageNodes[pageIndex];
        var arguments = target.FitArguments;
        return target.Fit switch
        {
            OutlineFit.Fit => [reference, new NameObject("Fit")],
            OutlineFit.FitHorizontal => [reference, new NameObject("FitH"), new NumberObject(arguments[0])],
            OutlineFit.FitVertical => [reference, new NameObject("FitV"), new NumberObject(arguments[0])],
            OutlineFit.FitBounding => [reference, new NameObject("FitB")],
            OutlineFit.FitBoundingHorizontal => [reference, new NameObject("FitBH"), new NumberObject(arguments[0])],
            OutlineFit.FitBoundingVertical => [reference, new NameObject("FitBV"), new NumberObject(arguments[0])],
            OutlineFit.Rectangle =>
            [
                reference,
                new NameObject("FitR"),
                new NumberObject(arguments[0]),
                new NumberObject(arguments[1]),
                new NumberObject(arguments[2]),
                new NumberObject(arguments[3]),
            ],
            OutlineFit.Coordinates =>
            [
                reference,
                new NameObject("XYZ"),
                new NumberObject(arguments[0]),
                new NumberObject(arguments[1]),
                new NumberObject(arguments[2]),
            ],
            _ => DestinationArray(reference, page.Height.Point),
        };
    }

    private static ArrayObject DestinationArray(ReferenceObject pageRef, double top) =>
    [
        pageRef,
        new NameObject("XYZ"),
        new NumberObject(0.0),
        new NumberObject(top),
        new NumberObject(0.0),
    ];

    public static ArrayObject BuildLinkAnnotations(DocumentWriter writer, IReadOnlyList<GeneratedLink> links)
    {
        var annots = new ArrayObject();
        foreach (var link in links)
        {
            ArrayObject rect =
            [
                new NumberObject(link.X1),
                new NumberObject(link.Y1),
                new NumberObject(link.X2),
                new NumberObject(link.Y2),
            ];

            ArrayObject border = [new NumberObject(0.0), new NumberObject(0.0), new NumberObject(0.0)];

            annots.Add(writer.Add(new DictionaryObject
            {
                ["Type"] = new NameObject("Annot"),
                ["Subtype"] = new NameObject("Link"),
                ["Rect"] = rect,
                ["Border"] = border,
                // ISO 19005-3 6.3.2: Print flag (bit 3 = 4) set, Hidden/NoView clear.
                ["F"] = new NumberObject(4),
                ["A"] = link.Destination is { } destination
                    ? new DictionaryObject
                    {
                        ["S"] = new NameObject("GoTo"),
                        ["D"] = new StringObject(destination),
                    }
                    : new DictionaryObject
                    {
                        ["S"] = new NameObject("URI"),
                        ["URI"] = new StringObject(link.Uri!),
                    },
            }));
        }

        return annots;
    }
}
