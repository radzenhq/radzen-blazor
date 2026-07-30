namespace Radzen.Documents.Layout;

internal readonly struct LinePlacementRequest
{
    public required int LinesThatFit { get; init; }

    public required int RemainingLines { get; init; }

    public required bool IsFirst { get; init; }

    public required bool HasPageContent { get; init; }

    public required int Widows { get; init; }

    public required int Orphans { get; init; }

    public required bool KeepTogether { get; init; }

    public required bool KeepWithNext { get; init; }

    public bool HasNextBlock { get; init; }

    public double AfterCursor { get; init; }

    public double NextBlockLeadingHeight { get; init; }

    public double ContentHeight { get; init; }
}

internal readonly struct LinePlacementDecision
{
    public int PlaceCount { get; init; }

    public bool MoveWhole { get; init; }
}

internal static class LinePlacer
{
    public static LinePlacementDecision Decide(in LinePlacementRequest r)
    {
        var k = r.LinesThatFit;
        var nrem = r.RemainingLines;
        var first = r.IsFirst;
        var hasPageContent = r.HasPageContent;

        var moveWhole = false;
        int placeCount;

        if (k >= nrem)
        {
            placeCount = nrem;
            if (first && r.KeepWithNext && hasPageContent && r.HasNextBlock &&
                r.AfterCursor + r.NextBlockLeadingHeight > r.ContentHeight + LayoutTolerance.Epsilon)
            {
                moveWhole = true;
            }
        }
        else if (first && r.KeepTogether)
        {
            moveWhole = true;
            placeCount = 0;
        }
        else if (!first)
        {
            var kept = k;
            if (nrem - kept < r.Widows)
            {
                kept = nrem - r.Widows;
            }

            placeCount = kept >= 1 ? kept : (k > 0 || hasPageContent ? k : 1);
        }
        else if (k < r.Orphans)
        {
            moveWhole = true;
            placeCount = 0;
        }
        else
        {
            var kept = k;
            if (nrem - kept < r.Widows)
            {
                kept = nrem - r.Widows;
            }

            if (kept < r.Orphans)
            {
                moveWhole = true;
                placeCount = 0;
            }
            else
            {
                placeCount = kept;
            }
        }

        if (placeCount == 0 && !moveWhole)
        {
            moveWhole = true;
        }

        if (moveWhole && !hasPageContent)
        {
            moveWhole = false;
            placeCount = k >= nrem ? nrem : (k > 0 ? k : 1);
        }

        return new LinePlacementDecision { PlaceCount = placeCount, MoveWhole = moveWhole };
    }
}
