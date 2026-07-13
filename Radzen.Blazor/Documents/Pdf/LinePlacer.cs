namespace Radzen.Documents.Pdf;

// The widow/orphan/keep-together/keep-with-next placement policy, lifted verbatim out of
// Paginator's paragraph loop into a pure decision so it can be unit-tested directly.
// Given how many of a paragraph's remaining lines physically fit and the keep settings,
// it returns how many lines to place now and whether the whole remainder moves to the
// next page - including the progress-guards that keep the paginator from stalling.
internal readonly struct LinePlacementRequest
{
    /// <summary>Lines from the current offset that physically fit in the remaining height.</summary>
    public required int LinesThatFit { get; init; }

    /// <summary>Lines still unplaced in the paragraph (from the current offset).</summary>
    public required int RemainingLines { get; init; }

    /// <summary>True while placing the paragraph's first slice (before any continuation break).</summary>
    public required bool IsFirst { get; init; }

    /// <summary>True when the page already holds content, so a move-whole is allowed.</summary>
    public required bool HasPageContent { get; init; }

    public required int Widows { get; init; }

    public required int Orphans { get; init; }

    public required bool KeepTogether { get; init; }

    public required bool KeepWithNext { get; init; }

    /// <summary>
    /// Keep-with-next look-ahead: true when a following block exists and its leading
    /// height was resolved. Only meaningful on the first slice of the last fitting group.
    /// </summary>
    public bool HasNextBlock { get; init; }

    /// <summary>Cursor after placing the paragraph's remaining lines plus its SpacingAfter.</summary>
    public double AfterCursor { get; init; }

    /// <summary>The next block's SpacingBefore plus its first-fragment height.</summary>
    public double NextBlockLeadingHeight { get; init; }

    /// <summary>The section body content height, the fit boundary for the look-ahead.</summary>
    public double ContentHeight { get; init; }
}

internal readonly struct LinePlacementDecision
{
    /// <summary>How many lines to place on the current page now.</summary>
    public int PlaceCount { get; init; }

    /// <summary>True to flush and re-place the whole remaining paragraph on a fresh page.</summary>
    public bool MoveWhole { get; init; }
}

internal static class LinePlacer
{
    private const double Eps = 1e-6;

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
                r.AfterCursor + r.NextBlockLeadingHeight > r.ContentHeight + Eps)
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

            // A continuation break still makes progress: never stall on an empty page
            // (a line taller than the page is placed alone) and never strand < 1 line.
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

        // A page must always make progress: placing zero lines (e.g. Orphans=0 with a
        // Widows pull-up) would flush a spurious blank page, so treat it as move-whole.
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
