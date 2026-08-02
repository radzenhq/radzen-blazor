using System.Collections.Generic;
using System.Collections.Immutable;
using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Layout;

internal readonly record struct LaidOutLayout(LaidOutDocument Scene, SourceResolver Sources);

internal static class DocumentLayouter
{
    public static LaidOutDocument Layout(Document document)
        => Layout(document, ImageProbes.None);

    public static LaidOutDocument Layout(Document document, ImageProbes probes)
        => LayoutWithSources(document, probes).Scene;

    public static LaidOutLayout LayoutWithSources(Document document)
        => LayoutWithSources(document, ImageProbes.None);

    public static LaidOutLayout LayoutWithSources(Document document, ImageProbes probes)
    {
        ArgumentNullException.ThrowIfNull(probes);
        var fonts = document.Fonts;
        var resolution = StyleResolver.Resolve(document);
        var first = LayoutPass(document, fonts, resolution, probes, null);

        if (!HasTableOfContents(document))
        {
            return Resolve(first, document, fonts);
        }

        var tocPages = AnchorPages(first.Pages);
        ValidateTocAnchors(document, tocPages);

        var entries = TocAnchors(document);
        var second = LayoutPass(document, fonts, resolution, probes, tocPages);
        var settled = AnchorPages(second.Pages);
        if (AnchorsStable(tocPages, settled, entries))
        {
            return Resolve(second, document, fonts);
        }

        var third = LayoutPass(document, fonts, resolution, probes, settled);
        if (!AnchorsStable(settled, AnchorPages(third.Pages), entries))
        {
            throw new InvalidOperationException(
                "Table of contents page numbers did not settle after three layout passes; " +
                "an entry keeps moving across a page boundary as the resolved numbers change its width.");
        }

        return Resolve(third, document, fonts);
    }

    private static LaidOutLayout Resolve(
        in LayoutPassResult pass,
        Document document,
        FontCollection fonts)
    {
        var scene = LayoutFinalizer.Resolve(
            new LaidOutDocument
            {
                Fonts = fonts.Snapshot(),
                Pages = pass.Pages,
                Semantics = pass.Semantics.Snapshot(),
                Info = GeometryCapture.DocumentInfo(document.Info),
            },
            fonts,
            pass.Lowering,
            pass.Capture);

        return new LaidOutLayout(scene, pass.Capture.Sources());
    }

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
        SemanticSnapshotBuilder Semantics,
        LoweringResult Lowering,
        LayoutCaptureContext Capture);

    private static LayoutPassResult LayoutPass(
        Document document,
        FontCollection fonts,
        StyleResolution resolution,
        ImageProbes probes,
        IReadOnlyDictionary<string, int>? tocPages)
    {
        var capture = new LayoutCaptureContext(probes);
        var semanticCapture = SemanticSnapshotBuilder.Capture(document, resolution, capture);
        var lowering = semanticCapture.Lowering;
        var pages = new List<LaidOutPage>();
        for (var index = 0; index < document.Sections.Count; index++)
        {
            var section = document.Sections[index];
            if (section.Direction != FlowDirection.LeftToRight || section.WritingMode != WritingMode.HorizontalTopToBottom)
            {
                throw new NotSupportedException("Right-to-left flow direction and vertical writing modes are not yet supported.");
            }

            foreach (var page in Paginator.Paginate(
                section,
                fonts,
                lowering,
                capture,
                tocPages,
                pages.Count,
                index))
            {
                pages.Add(page);
            }
        }

        lowering.Semantics.Seal();

        return new LayoutPassResult(
            [.. pages],
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
                            $"Table of contents entry anchor '{entry.Anchor}' does not exist; set Inline.Anchor on the destination inline.");
                    }
                }
            }
        }
    }

    private static Dictionary<string, int> AnchorPages(ImmutableArray<LaidOutPage> pages)
    {
        var anchors = new Dictionary<string, int>(StringComparer.Ordinal);
        var seen = new Dictionary<string, SourceId>(StringComparer.Ordinal);
        for (var i = 0; i < pages.Length; i++)
        {
            foreach (var anchor in PageNavigationCollector.Anchors(pages[i], seen))
            {
                anchors.Add(anchor.Name, i + 1);
            }
        }

        return anchors;
    }
}
