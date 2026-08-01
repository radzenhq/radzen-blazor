#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents;

namespace Radzen.Blazor.Tests.Isolated;

internal static class IsolatedPaginator
{
    public static ImmutableArray<LaidOutPage> PaginateIsolated(
        Section section,
        FontCollection fonts,
        LayoutCaptureContext? capture = null)
        => Radzen.Documents.Layout.Paginator.Paginate(
            section,
            fonts,
            LoweringResult.CreateForDocument(StyleResolution.Empty),
            capture ?? new LayoutCaptureContext(ImageProbes.None),
            tocPages: null);
}

internal static class IsolatedTableLayout
{
    public static LaidOutTable LayoutIsolated(
        Table table,
        double availableWidth,
        FontCollection fonts,
        LayoutCaptureContext? capture = null)
        => Radzen.Documents.Layout.TableLayout.Layout(
            table,
            availableWidth,
            fonts,
            LoweringResult.CreateForDocument(StyleResolution.Empty),
            capture ?? new LayoutCaptureContext(ImageProbes.None));
}

internal static class IsolatedTablePaginator
{
    public static IReadOnlyList<LaidOutTableSlice> Paginate(
        LaidOutTable layout,
        Table source,
        double availableHeight,
        LayoutCaptureContext? capture = null)
        => Radzen.Documents.Layout.TablePaginator.Paginate(
            layout,
            source,
            availableHeight,
            capture ?? new LayoutCaptureContext(ImageProbes.None));

    public static IReadOnlyList<LaidOutTableSlice> Paginate(
        LaidOutTable layout,
        Table source,
        double firstAvailable,
        double subsequentAvailable,
        LayoutCaptureContext? capture = null)
        => Radzen.Documents.Layout.TablePaginator.Paginate(
            layout,
            source,
            firstAvailable,
            subsequentAvailable,
            capture ?? new LayoutCaptureContext(ImageProbes.None));
}

internal static class IsolatedLineBreaker
{
    public static IReadOnlyList<LineBox> Break(
        Paragraph paragraph,
        double maxWidthPoints,
        FontCollection fonts,
        HorizontalAlignment? inheritedAlignment = null,
        LoweringResult? resolution = null,
        LayoutCaptureContext? capture = null)
        => LineLayouter.Layout(
            paragraph,
            maxWidthPoints,
            fonts,
            capture ?? new LayoutCaptureContext(ImageProbes.None),
            inheritedAlignment,
            resolution);
}

internal static class IsolatedBlockExpander
{
    public static ExpandedBlocks ExpandBlocksIsolated(
        BlockCollection blocks,
        double availableWidth,
        bool keepSpecialContainers = false,
        IReadOnlyDictionary<string, int>? tocPages = null,
        FontCollection? fonts = null)
        => Radzen.Documents.Layout.BlockExpander.ExpandBlocks(
            blocks,
            availableWidth,
            LoweringResult.CreateForDocument(StyleResolution.Empty),
            keepSpecialContainers,
            tocPages,
            fonts);
}
