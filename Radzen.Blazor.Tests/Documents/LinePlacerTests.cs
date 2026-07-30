#nullable enable
using Xunit;

using Radzen.Documents;
using Radzen.Documents.Layout;
namespace Radzen.Blazor.Documents.Tests;

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
        var d = Decide(fit: 7, remaining: 8, orphans: 2, widows: 2);
        Assert.False(d.MoveWhole);
        Assert.Equal(6, d.PlaceCount);
    }

    [Fact]
    public void WidowPullUp_DropsBelowOrphans_MovesWhole()
    {
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
        var d = Decide(fit: 4, remaining: 6, keepTogether: true, hasPageContent: false);
        Assert.False(d.MoveWhole);
        Assert.Equal(4, d.PlaceCount);
    }

    [Fact]
    public void KeepWithNext_NextBlockOverflows_MovesHeadingToNextPage()
    {
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
        var d = Decide(fit: 0, remaining: 4, first: false, hasPageContent: false, widows: 2);
        Assert.False(d.MoveWhole);
        Assert.Equal(1, d.PlaceCount);
    }

    [Fact]
    public void Continuation_NotEnoughRoom_PageHasContent_MovesWhole()
    {
        var d = Decide(fit: 0, remaining: 4, first: false, hasPageContent: true, widows: 2);
        Assert.True(d.MoveWhole);
        Assert.Equal(0, d.PlaceCount);
    }

    [Fact]
    public void Continuation_WidowPullUp_KeepsAtLeastOne()
    {
        var d = Decide(fit: 3, remaining: 4, first: false, widows: 2);
        Assert.False(d.MoveWhole);
        Assert.Equal(2, d.PlaceCount);
    }

    [Fact]
    public void ZeroOrphansWidowPullUp_WouldPlaceZero_TreatedAsMoveWhole()
    {
        var d = Decide(fit: 1, remaining: 2, orphans: 0, widows: 2, hasPageContent: true);
        Assert.True(d.MoveWhole);
        Assert.Equal(0, d.PlaceCount);
    }
}
