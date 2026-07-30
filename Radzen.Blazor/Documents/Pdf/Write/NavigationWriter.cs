using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Emission;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Documents.Pdf.Write;

internal sealed class NavigationWriter(PortableDocument document)
{
    public void WriteDestinations(
        DocumentWriter writer,
        DictionaryObject catalog,
        List<(Page Page, DictionaryObject Node, ReferenceObject Reference)> pageNodes,
        EmissionPageMap pageMap)
    {
        var sorted = new SortedDictionary<string, EmissionAnchor>(StringComparer.Ordinal);
        if (document.EmissionPlan is { } plan)
        {
            foreach (var anchor in plan.Anchors)
            {
                sorted.Add(anchor.Key, anchor.Value);
            }
        }

        NameTree.AddCategory(writer, catalog, "Dests",
            sorted.Select(entry => (entry.Key,
                (DocumentObject)DestinationArray(
                    pageNodes[pageMap.IndexOf(entry.Value.Page, $"named destination '{entry.Key}'")].Reference,
                    entry.Value.Top))));
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
                ["Title"] = StringObject.FromText(item.Title),
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
            if (document.EmissionPlan?.Anchors.ContainsKey(anchor) != true)
            {
                throw new InvalidOperationException($"Outline target anchor '{anchor}' does not exist; set Inline.Anchor on the destination inline.");
            }

            return new StringObject(anchor);
        }

        var pageIndex = target.PageIndex ?? 0;
        if (pageIndex >= pageNodes.Count)
        {
            throw new InvalidOperationException($"Outline target page index {pageIndex} is out of range; the document has {pageNodes.Count} pages.");
        }

        var (page, _, reference) = pageNodes[pageIndex];
        return DestinationWriter.Write(target, reference, DestinationArray(reference, page.Height.Point));
    }

    private static ArrayObject DestinationArray(ReferenceObject pageRef, double top) =>
    [
        pageRef,
        new NameObject("XYZ"),
        new NumberObject(0.0),
        new NumberObject(top),
        new NumberObject(0.0),
    ];

    public static LinkAnnotationEmission BuildLinkAnnotations(
        DocumentWriter writer, IReadOnlyList<EmissionLink> links, int pageIndex)
    {
        var annots = new ArrayObject();
        var joins = new List<AnnotationElementJoin>();
        foreach (var link in links)
        {
            var annotation = new DictionaryObject
            {
                ["Type"] = new NameObject("Annot"),
                ["Subtype"] = new NameObject("Link"),
                ["Rect"] = PageResourceBuilder.NumberBox(new PdfRect(link.X1, link.Y1, link.X2, link.Y2)),
                ["Border"] = new ArrayObject { new NumberObject(0.0), new NumberObject(0.0), new NumberObject(0.0) },
                // ISO 19005-3 6.3.2: Print flag (bit 3 = 4) set, Hidden/NoView clear.
                ["F"] = new NumberObject(4),
                ["A"] = link.Destination is { } destination
                    ? LinkAction.GoTo(new StringObject(destination))
                    : LinkAction.Uri(link.Uri!),
            };

            var reference = writer.Add(annotation);
            annots.Add(reference);
            if (link.StructureElementId is { } elementId)
            {
                joins.Add(new AnnotationElementJoin(elementId, pageIndex, annotation, reference));
            }
        }

        return new LinkAnnotationEmission(annots, joins);
    }
}

internal sealed record LinkAnnotationEmission(
    ArrayObject Annotations,
    IReadOnlyList<AnnotationElementJoin> StructureJoins);

internal readonly record struct AnnotationElementJoin(
    int StructureElementId,
    int PageIndex,
    DictionaryObject Annotation,
    ReferenceObject Reference);

internal static class LinkAction
{
    public static DictionaryObject Uri(string uri) => new()
    {
        ["S"] = new NameObject("URI"),
        ["URI"] = new StringObject(uri),
    };

    public static DictionaryObject GoTo(DocumentObject destination) => new()
    {
        ["S"] = new NameObject("GoTo"),
        ["D"] = destination,
    };
}
