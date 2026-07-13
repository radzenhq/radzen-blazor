#nullable enable
using Xunit;
using Radzen.Documents.Pdf;

using Radzen.Documents.Pdf.Emit;
namespace Radzen.Blazor.Pdf.Tests;

// Direct unit tests for the extracted widow/orphan/keep-together/keep-with-next placement
// policy. These assert the returned (PlaceCount, MoveWhole) decision rather than end-to-end
// PDF output, pinning the exact branch and progress-guard behavior lifted out of Paginator.
public class LinePlacerTests
{
    private static LinePlacementDecision Decide(
        int fit,
        int remaining,
        bool first = true,
        bool hasPageContent = true,
        int widows = 2,
        int orphans = 2,
        bool keepTogether = false,
        bool keepWithNext = false,
        bool hasNextBlock = false,
        double afterCursor = 0,
        double nextLeadingHeight = 0,
        double contentHeight = 0)
        => LinePlacer.Decide(new LinePlacementRequest
        {
            LinesThatFit = fit,
            RemainingLines = remaining,
            IsFirst = first,
            HasPageContent = hasPageContent,
            Widows = widows,
            Orphans = orphans,
            KeepTogether = keepTogether,
            KeepWithNext = keepWithNext,
            HasNextBlock = hasNextBlock,
            AfterCursor = afterCursor,
            NextBlockLeadingHeight = nextLeadingHeight,
            ContentHeight = contentHeight,
        });

    [Fact]
    public void WholeParagraphFits_PlacesAll()
    {
        var d = Decide(fit: 5, remaining: 5);
        Assert.Equal(5, d.PlaceCount);
        Assert.False(d.MoveWhole);
    }

    [Fact]
    public void FirstSlice_FewerThanOrphansFit_MovesWhole()
    {
        // 1 line fits but Orphans=2 requires at least two at the bottom.
        var d = Decide(fit: 1, remaining: 6, orphans: 2);
        Assert.True(d.MoveWhole);
        Assert.Equal(0, d.PlaceCount);
    }

    [Fact]
    public void FirstSlice_EnoughOrphans_PlacesFittingLines()
    {
        var d = Decide(fit: 3, remaining: 8, orphans: 2, widows: 2);
        Assert.False(d.MoveWhole);
        Assert.Equal(3, d.PlaceCount);
    }

    [Fact]
    public void WidowPullUp_ReducesPlacedSoTwoCarryOver()
    {
        // 7 of 8 fit; leaving 1 would violate Widows=2, so keep only 6.
        var d = Decide(fit: 7, remaining: 8, orphans: 2, widows: 2);
        Assert.False(d.MoveWhole);
        Assert.Equal(6, d.PlaceCount);
    }

    [Fact]
    public void WidowPullUp_DropsBelowOrphans_MovesWhole()
    {
        // 2 of 3 fit; a widow pull-up would strand 0 orphans, so the whole block moves.
        var d = Decide(fit: 2, remaining: 3, orphans: 2, widows: 2);
        Assert.True(d.MoveWhole);
        Assert.Equal(0, d.PlaceCount);
    }

    [Fact]
    public void KeepTogether_DoesNotFitWhole_MovesWhole()
    {
        var d = Decide(fit: 4, remaining: 6, keepTogether: true);
        Assert.True(d.MoveWhole);
        Assert.Equal(0, d.PlaceCount);
    }

    [Fact]
    public void KeepTogether_OnEmptyPage_PlacesFittingLines()
    {
        // Nothing on the page yet: move-whole would stall, so the guard places what fits.
        var d = Decide(fit: 4, remaining: 6, keepTogether: true, hasPageContent: false);
        Assert.False(d.MoveWhole);
        Assert.Equal(4, d.PlaceCount);
    }

    [Fact]
    public void KeepWithNext_NextBlockOverflows_MovesHeadingToNextPage()
    {
        // The heading fits whole but its following block would spill past the page bottom.
        var d = Decide(
            fit: 2,
            remaining: 2,
            keepWithNext: true,
            hasNextBlock: true,
            afterCursor: 90,
            nextLeadingHeight: 20,
            contentHeight: 100);
        Assert.True(d.MoveWhole);
        Assert.Equal(2, d.PlaceCount);
    }

    [Fact]
    public void KeepWithNext_NextBlockFits_StaysPut()
    {
        var d = Decide(
            fit: 2,
            remaining: 2,
            keepWithNext: true,
            hasNextBlock: true,
            afterCursor: 60,
            nextLeadingHeight: 20,
            contentHeight: 100);
        Assert.False(d.MoveWhole);
        Assert.Equal(2, d.PlaceCount);
    }

    [Fact]
    public void KeepWithNext_OnEmptyPage_DoesNotEngage()
    {
        // The caller only resolves the next block when the page has content; with no
        // page content the look-ahead is inert and the heading places.
        var d = Decide(
            fit: 2,
            remaining: 2,
            keepWithNext: true,
            hasPageContent: false,
            hasNextBlock: false,
            afterCursor: 90,
            nextLeadingHeight: 20,
            contentHeight: 100);
        Assert.False(d.MoveWhole);
        Assert.Equal(2, d.PlaceCount);
    }

    [Fact]
    public void Continuation_NotEnoughRoom_EmptyPage_PlacesOneLine()
    {
        // A continuation slice on a fresh page where not even one line fits: the
        // progress-guard forces a single oversized line rather than looping forever.
        var d = Decide(fit: 0, remaining: 4, first: false, hasPageContent: false, widows: 2);
        Assert.False(d.MoveWhole);
        Assert.Equal(1, d.PlaceCount);
    }

    [Fact]
    public void Continuation_NotEnoughRoom_PageHasContent_MovesWhole()
    {
        // On a page that already has content, a continuation that fits nothing yields a
        // zero placeCount, which the progress-guard converts to move-whole so the loop
        // flushes and retries on a fresh page instead of stranding a line.
        var d = Decide(fit: 0, remaining: 4, first: false, hasPageContent: true, widows: 2);
        Assert.True(d.MoveWhole);
        Assert.Equal(0, d.PlaceCount);
    }

    [Fact]
    public void Continuation_WidowPullUp_KeepsAtLeastOne()
    {
        // 3 of 4 fit on a continuation; Widows=2 would pull to 2 kept.
        var d = Decide(fit: 3, remaining: 4, first: false, widows: 2);
        Assert.False(d.MoveWhole);
        Assert.Equal(2, d.PlaceCount);
    }

    [Fact]
    public void ZeroOrphansWidowPullUp_WouldPlaceZero_TreatedAsMoveWhole()
    {
        // Orphans=0 with a Widows pull-up strands the fit line (kept -> 0) without setting
        // move-whole; on a page that already has content the guard converts placeCount 0
        // into move-whole so no spurious blank page is flushed.
        var d = Decide(fit: 1, remaining: 2, orphans: 0, widows: 2, hasPageContent: true);
        Assert.True(d.MoveWhole);
        Assert.Equal(0, d.PlaceCount);
    }
}
