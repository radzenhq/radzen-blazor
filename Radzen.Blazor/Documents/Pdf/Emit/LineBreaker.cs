using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;


internal struct LineFragment
{
    public required Run Run { get; init; }

    // The resolved font of this fragment (the run's font run through the style cascade, or the
    // synthesized font of a marker/hyphen/leader fragment). Carried on the display-list layer so
    // emission never reads a resolved font off the shared authoring model.
    public required Font Font { get; init; }

    public required string Text { get; init; }

    public required int Start { get; init; }

    public required int Length { get; init; }

    public double XOffset { get; set; }

    public required double Advance { get; init; }

    // A list-item marker fragment; in tagged output it is tagged into the item's Lbl element.
    public bool IsMarker { get; init; }
}

internal sealed class LineBox
{
    public required IReadOnlyList<LineFragment> Fragments { get; init; }

    public double Width { get; set; }

    public double Height { get; set; }

    public double Baseline { get; set; }
}

internal sealed class LineLayoutContext
{
    public required Paragraph Paragraph { get; init; }

    public required FontCollection Fonts { get; init; }

    public required double MaxWidth { get; init; }

    public required double Indent { get; init; }

    public HorizontalAlignment? InheritedAlignment { get; init; }

    public StyleResolution? Resolution { get; init; }

    public List<TabStop>? SortedTabStops { get; init; }
}

internal readonly record struct LineBuildRequest(int First, int Last, bool IsLast, bool IncludeMarker);

internal static class LineBreaker
{
    public static IReadOnlyList<LineBox> Break(
        Paragraph paragraph,
        double maxWidthPoints,
        FontCollection fonts,
        HorizontalAlignment? inheritedAlignment = null,
        StyleResolution? resolution = null)
        => LineLayouter.Layout(paragraph, maxWidthPoints, fonts, inheritedAlignment, resolution);

    internal static double AdvanceToTabStop(double position) => LineLayouter.AdvanceToTabStop(position);
}
