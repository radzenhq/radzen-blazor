using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Fonts;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal static class DocumentLayouter
{
    public static LaidOutDocument Layout(Document document)
        => Layout(document, ImageProbe.Registered);

    public static LaidOutDocument Layout(Document document, ImageProbes probes)
    {
        ArgumentNullException.ThrowIfNull(probes);
        return Layout(document, document.Fonts, StyleResolver.Resolve(document), probes.Measure);
    }

    public static LaidOutDocument Layout(
        Document document,
        FontCollection fonts,
        StyleResolution resolution,
        Func<Image, double, (double Width, double Height)>? measureImage)
    {
        var first = LayoutPass(document, fonts, resolution, measureImage, null);

        if (!HasTableOfContents(document))
        {
            return Resolve(first, document, fonts);
        }

        var tocPages = AnchorPages(first.Pages);
        ValidateTocAnchors(document, tocPages);

        var entries = TocAnchors(document);
        var second = LayoutPass(document, fonts, resolution, measureImage, tocPages);
        var settled = AnchorPages(second.Pages);
        if (AnchorsStable(tocPages, settled, entries))
        {
            return Resolve(second, document, fonts);
        }

        var third = LayoutPass(document, fonts, resolution, measureImage, settled);
        if (!AnchorsStable(settled, AnchorPages(third.Pages), entries))
        {
            throw new InvalidOperationException(
                "Table of contents page numbers did not settle after three layout passes; " +
                "an entry keeps moving across a page boundary as the resolved numbers change its width.");
        }

        return Resolve(third, document, fonts);
    }

    private static LaidOutDocument Resolve(in LayoutPassResult pass, Document document, FontCollection fonts)
        => LayoutFinalizer.Resolve(
            new LaidOutDocument
            {
                Id = pass.Id,
                Fonts = fonts.Snapshot(),
                Pages = pass.Pages,
                Semantics = pass.Semantics.Snapshot(),
                Info = GeometryCapture.DocumentInfo(document.Info),
            },
            fonts,
            pass.Lowering,
            pass.Capture);

    internal static bool AnchorsStable(
        IReadOnlyDictionary<string, int> previous,
        IReadOnlyDictionary<string, int> current,
        IReadOnlyCollection<string> anchors)
    {
        foreach (var anchor in anchors)
        {
            if (!previous.TryGetValue(anchor, out var before)
                || !current.TryGetValue(anchor, out var after)
                || before != after)
            {
                return false;
            }
        }

        return true;
    }

    private static HashSet<string> TocAnchors(Document document)
    {
        var anchors = new HashSet<string>(StringComparer.Ordinal);
        foreach (var section in document.Sections)
        {
            foreach (var block in section.Blocks)
            {
                if (block is not TableOfContents toc)
                {
                    continue;
                }

                foreach (var entry in toc.Entries)
                {
                    anchors.Add(entry.Anchor);
                }
            }
        }

        return anchors;
    }

    private readonly record struct LayoutPassResult(
        ImmutableArray<LaidOutPage> Pages,
        LaidOutNodeId Id,
        SemanticSnapshotBuilder Semantics,
        LoweringContext Lowering,
        LayoutCaptureContext Capture);

    private static LayoutPassResult LayoutPass(
        Document document,
        FontCollection fonts,
        StyleResolution resolution,
        Func<Image, double, (double Width, double Height)>? measureImage,
        IReadOnlyDictionary<string, int>? tocPages)
    {
        var capture = new LayoutCaptureContext();
        var semanticCapture = SemanticSnapshotBuilder.Capture(document, resolution, capture);
        var lowering = semanticCapture.Lowering;
        var pages = new List<LaidOutPage>();
        foreach (var section in document.Sections)
        {
            if (section.Direction != FlowDirection.LeftToRight || section.WritingMode != WritingMode.HorizontalTopToBottom)
            {
                throw new NotSupportedException("Right-to-left flow direction and vertical writing modes are not yet supported.");
            }

            foreach (var page in Paginator.Paginate(
                section,
                fonts,
                lowering,
                measureImage,
                tocPages,
                capture,
                pages.Count))
            {
                pages.Add(page);
            }
        }

        return new LayoutPassResult(
            [.. pages],
            capture.Node(),
            semanticCapture.Builder,
            lowering,
            capture);
    }

    private static bool HasTableOfContents(Document document)
    {
        foreach (var section in document.Sections)
        {
            foreach (var block in section.Blocks)
            {
                if (block is TableOfContents)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static void ValidateTocAnchors(Document document, Dictionary<string, int> tocPages)
    {
        foreach (var section in document.Sections)
        {
            foreach (var block in section.Blocks)
            {
                if (block is not TableOfContents toc)
                {
                    continue;
                }

                foreach (var entry in toc.Entries)
                {
                    if (!tocPages.ContainsKey(entry.Anchor))
                    {
                        throw new InvalidOperationException(
                            $"Table of contents entry anchor '{entry.Anchor}' does not exist; set Run.Anchor on the destination run.");
                    }
                }
            }
        }
    }

    private static Dictionary<string, int> AnchorPages(ImmutableArray<LaidOutPage> pages)
    {
        var anchors = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < pages.Length; i++)
        {
            var page = pages[i];
            CollectLayer(anchors, page.Body, i + 1);
            CollectLayer(anchors, page.HeaderLayer, i + 1);
            CollectLayer(anchors, page.FooterLayer, i + 1);
        }

        return anchors;
    }

    private static void CollectLayer(Dictionary<string, int> anchors, LaidOutLayer layer, int page)
    {
        foreach (var line in layer.Lines)
        {
            CollectLine(anchors, line.Line, page);
        }

        foreach (var table in layer.Tables)
        {
            CollectFragment(anchors, table, page);
        }

        foreach (var box in layer.Boxes)
        {
            CollectBoxContent(anchors, box.Content, page);
        }
    }

    private static void CollectFragment(Dictionary<string, int> anchors, in LaidOutTableFragment positioned, int page)
    {
        foreach (var row in positioned.Rows)
        {
            foreach (var placed in row.Cells)
            {
                CollectCell(anchors, placed.Cell, page);
            }
        }
    }

    private static void CollectCell(Dictionary<string, int> anchors, LaidOutCell cell, int page)
    {
        foreach (var line in cell.Lines)
        {
            CollectLine(anchors, line.Line, page);
        }

        CollectNested(anchors, cell.Tables, cell.Boxes, page);
    }

    private static void CollectBoxContent(Dictionary<string, int> anchors, in LaidOutBoxContent content, int page)
    {
        foreach (var line in content.Lines)
        {
            CollectLine(anchors, line.Line, page);
        }

        CollectNested(anchors, content.Tables, content.Boxes, page);
    }

    private static void CollectNested(
        Dictionary<string, int> anchors,
        ImmutableArray<LaidOutTablePlacement> tables,
        ImmutableArray<LaidOutBox> boxes,
        int page)
    {
        foreach (var nested in tables)
        {
            foreach (var cell in nested.Layout.Cells)
            {
                CollectCell(anchors, cell, page);
            }
        }

        foreach (var nested in boxes)
        {
            CollectBoxContent(anchors, nested.Content, page);
        }
    }

    private static void CollectLine(Dictionary<string, int> anchors, LineBox line, int page)
    {
        foreach (var fragment in line.Fragments)
        {
            if (fragment.Paint.Anchor is { Length: > 0 } anchor)
            {
                anchors.TryAdd(anchor, page);
            }
        }
    }
}
