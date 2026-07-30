#nullable enable
using System.Collections.Generic;
using System.Collections.Immutable;
using System;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents;

namespace Radzen.Blazor.Documents.Tests;

internal static class IsolatedPaginator
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

internal static class IsolatedTableLayout
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

internal static class IsolatedLineBreaker
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
