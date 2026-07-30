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
            measureImage,
            capture: capture);
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
            capture: capture);
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
