#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Geometry;
using Radzen.Documents.Layout;

namespace Radzen.Blazor.Pdf.Tests;

internal static class Paginator
{
    public static ImmutableArray<LaidOutPage> PaginateIsolated(
        Section section,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage = null,
        LayoutCaptureContext? capture = null)
        => Radzen.Documents.Layout.Paginator.Paginate(
            section,
            fonts,
            LoweringContext.CreateForDocument(StyleResolution.Empty),
            capture ?? new LayoutCaptureContext(),
            measureImage,
            tocPages: null);
}

internal static class TableLayout
{
    public static LaidOutTable LayoutIsolated(
        Table table,
        double availableWidth,
        FontCollection fonts,
        Func<Image, double, (double Width, double Height)>? measureImage = null,
        LayoutCaptureContext? capture = null)
        => Radzen.Documents.Layout.TableLayout.Layout(
            table,
            availableWidth,
            fonts,
            measureImage,
            LoweringContext.CreateForDocument(StyleResolution.Empty),
            capture ?? new LayoutCaptureContext());
}

internal static class TablePaginator
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
            capture ?? new LayoutCaptureContext());

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
            capture ?? new LayoutCaptureContext());
}

internal static class LineBreaker
{
    public static IReadOnlyList<LineBox> Break(
        Paragraph paragraph,
        double maxWidthPoints,
        FontCollection fonts,
        HorizontalAlignment? inheritedAlignment = null,
        LoweringContext? resolution = null,
        LayoutCaptureContext? capture = null)
        => LineLayouter.Layout(
            paragraph,
            maxWidthPoints,
            fonts,
            capture ?? new LayoutCaptureContext(),
            inheritedAlignment,
            resolution);
}

internal static class BlockExpander
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
            LoweringContext.CreateForDocument(StyleResolution.Empty),
            keepSpecialContainers,
            tocPages,
            fonts);
}
