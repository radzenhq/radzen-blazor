using System.Collections.Generic;
using Radzen.Documents.Fonts;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;


internal sealed class LineLayoutContext
{
    public required Paragraph Paragraph { get; init; }

    public required FontCollection Fonts { get; init; }

    public required double MaxWidth { get; init; }

    public required double Indent { get; init; }

    public HorizontalAlignment? InheritedAlignment { get; init; }

    public LoweringContext? Resolution { get; init; }

    public List<TabStop>? SortedTabStops { get; init; }

    public required LayoutCaptureContext Capture { get; init; }
}

internal readonly record struct LineBuildRequest(int First, int Last, bool IsLast, bool IncludeMarker);

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
            inheritedAlignment,
            resolution,
            capture);
}
