#nullable enable
using System;
using System.Collections;
using System.Linq;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class ModelCollectionPublicContractTests
{
    [Fact]
    public void TabStopsAddInsertIndexAndEnumerateInOrder()
    {
        var stops = new TabStopCollection();
        var first = stops.Add(Unit.Parse("10pt"));
        var third = stops.Add(new TabStop(Unit.Parse("30pt"), TabAlignment.Right, '.'));
        var second = stops.Insert(1, new TabStop(Unit.Parse("20pt"), TabAlignment.Center));

        Assert.Same(first, stops[0]);
        Assert.Same(second, stops[1]);
        Assert.Same(third, stops[2]);
        Assert.Equal(3, stops.Count);
        Assert.Equal(new[] { first, second, third }, stops.ToArray());
        Assert.Equal(new[] { first, second, third }, ((IEnumerable)stops).Cast<TabStop>().ToArray());
        Assert.Equal(Unit.Parse("30pt"), third.Position);
        Assert.Equal(TabAlignment.Right, third.Alignment);
        Assert.Equal('.', third.Leader);
    }

    [Fact]
    public void TabStopsRemoveRemoveAtAndClear()
    {
        var stops = new TabStopCollection();
        var first = stops.Add(Unit.Parse("10pt"));
        var second = stops.Add(Unit.Parse("20pt"));
        var absent = new TabStop(Unit.Parse("30pt"));

        Assert.True(stops.Remove(first));
        Assert.False(stops.Remove(absent));
        stops.RemoveAt(0);
        Assert.Empty(stops);

        stops.Add(second);
        stops.Clear();
        Assert.Empty(stops);
    }

    [Fact]
    public void TabStopsRejectNullArguments()
    {
        var stops = new TabStopCollection();

        Assert.Throws<ArgumentNullException>(() => stops.Add(null!));
        Assert.Throws<ArgumentNullException>(() => stops.Insert(0, null!));
        Assert.Throws<ArgumentNullException>(() => stops.Remove(null!));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void TabStopsInsertRejectsOutOfRangeIndexes(int index)
        => Assert.Throws<ArgumentOutOfRangeException>(
            () => new TabStopCollection().Insert(index, new TabStop(Unit.Parse("10pt"))));

    [Fact]
    public void TocEntriesAddInsertMoveIndexAndEnumerateInOrder()
    {
        var entries = new TocEntryCollection();
        var first = entries.Add(new TocEntry("First", "first"));
        var third = entries.Add(new TocEntry("Third", "third", 2));
        var second = entries.Insert(1, new TocEntry("Second", "second", 1));

        entries.Move(2, 0);

        Assert.Equal(3, entries.Count);
        Assert.Same(third, entries[0]);
        Assert.Same(first, entries[1]);
        Assert.Same(second, entries[2]);
        Assert.Equal(new[] { third, first, second }, entries.ToArray());
        Assert.Equal(new[] { third, first, second }, ((IEnumerable)entries).Cast<TocEntry>().ToArray());
        Assert.Equal("Third", third.Text);
        Assert.Equal("third", third.Anchor);
        Assert.Equal(2, third.Level);
    }

    [Fact]
    public void TocEntryHasSingleParentUntilRemoved()
    {
        var first = new TocEntryCollection();
        var second = new TocEntryCollection();
        var entry = first.Add(new TocEntry("Entry", "entry"));

        Assert.Throws<InvalidOperationException>(() => first.Add(entry));
        Assert.Throws<InvalidOperationException>(() => second.Add(entry));

        Assert.True(first.Remove(entry));
        Assert.Same(entry, second.Add(entry));
    }

    [Fact]
    public void TocEntriesRemoveAtAndClearDetachEntries()
    {
        var first = new TocEntryCollection();
        var second = new TocEntryCollection();
        var removed = first.Add(new TocEntry("Removed", "removed"));
        var cleared = first.Add(new TocEntry("Cleared", "cleared"));

        first.RemoveAt(0);
        Assert.Same(removed, second.Add(removed));

        first.Clear();
        Assert.Same(cleared, second.Add(cleared));
        Assert.Empty(first);
    }

    [Fact]
    public void TocEntriesRejectNullArguments()
    {
        var entries = new TocEntryCollection();

        Assert.Throws<ArgumentNullException>(() => entries.Add(null!));
        Assert.Throws<ArgumentNullException>(() => entries.Insert(0, null!));
        Assert.Throws<ArgumentNullException>(() => entries.Remove(null!));
    }

    [Theory]
    [InlineData(null, "anchor", 0)]
    [InlineData("text", null, 0)]
    [InlineData("text", "anchor", -1)]
    public void TocEntryConstructorValidatesArguments(string? text, string? anchor, int level)
        => Assert.ThrowsAny<ArgumentException>(() => new TocEntry(text!, anchor!, level));

    [Fact]
    public void TocEntryCollectionDoesNotExposePublicChangeTracking()
    {
        Assert.Null(typeof(TocEntryCollection).GetProperty("IsModified"));
        Assert.Null(typeof(TocEntryCollection).GetMethod("AcceptChanges"));
    }
}
