using System.Collections.Generic;
using Radzen;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class VirtualItemsTests
    {
        static List<string> Rows(params string[] rows) => new(rows);

        [Fact]
        public void ANewerFetchPublishesOverAnOlderOne()
        {
            var window = new VirtualItems<string>();

            var first = window.BeginFetch();
            var second = window.BeginFetch();

            Assert.True(window.TryPublish(second, 0, 1, Rows("new")));
            Assert.True(window.TryPublish(first, 0, 1, Rows("old")) == false);

            Assert.True(window.TryGetAt(0, out var item));
            Assert.Equal("new", item);
        }

        [Fact]
        public void ASupersededFetchCannotPublishAfterTheNewerOneHas()
        {
            var window = new VirtualItems<string>();

            var stale = window.BeginFetch();
            var current = window.BeginFetch();

            window.TryPublish(current, 0, 3, Rows("a", "b", "c"));

            Assert.False(window.TryPublish(stale, 0, 0, Rows()));

            Assert.True(window.HasAny);
            Assert.Equal(3, window.TotalCount);
            Assert.True(window.TryGetAt(0, out var first));
            Assert.True(window.TryGetAt(2, out var last));
            Assert.Equal("a", first);
            Assert.Equal("c", last);
        }

        [Fact]
        public void AFetchCannotPublishOnceANewerOneHasBegun()
        {
            var window = new VirtualItems<string>();

            var older = window.BeginFetch();
            var newer = window.BeginFetch();

            Assert.False(window.TryPublish(older, 0, 1, Rows("older")));
            Assert.False(window.TryGetAt(0, out _));

            Assert.True(window.TryPublish(newer, 0, 1, Rows("newer")));
            Assert.True(window.TryGetAt(0, out var item));
            Assert.Equal("newer", item);
        }

        [Fact]
        public void InvalidatingMovesTheQueryGeneration()
        {
            var window = new VirtualItems<string>();

            var generation = window.QueryGeneration;

            window.BeginFetch();

            Assert.Equal(generation, window.QueryGeneration);

            window.Invalidate();

            Assert.NotEqual(generation, window.QueryGeneration);
        }

        [Fact]
        public void InvalidatingBarsTheFetchesAlreadyInFlight()
        {
            var window = new VirtualItems<string>();

            var inFlight = window.BeginFetch();

            window.TryPublish(inFlight, 0, 2, Rows("a", "b"));

            var alsoInFlight = window.BeginFetch();
            window.Invalidate();

            Assert.False(window.TryGetAt(0, out _));
            Assert.False(window.TryPublish(alsoInFlight, 0, 2, Rows("stale", "stale")));
            Assert.False(window.TryGetAt(0, out _));

            var replacement = window.BeginFetch();

            Assert.True(window.TryPublish(replacement, 0, 1, Rows("fresh")));
            Assert.True(window.TryGetAt(0, out var item));
            Assert.Equal("fresh", item);
        }

        [Fact]
        public void HasAnyAssumesRowsUntilAFetchSaysOtherwise()
        {
            var window = new VirtualItems<string>();

            Assert.True(window.HasAny);

            var fetch = window.BeginFetch();
            window.TryPublish(fetch, 0, 0, Rows());

            Assert.False(window.HasAny);

            window.Invalidate();

            Assert.True(window.HasAny);
        }

        [Fact]
        public void TryGetAtResolvesAnIndexWithinTheFetchedWindow()
        {
            var window = new VirtualItems<string>();

            var fetch = window.BeginFetch();
            window.TryPublish(fetch, 10, 40, Rows("ten", "eleven", "twelve"));

            Assert.True(window.TryGetAt(11, out var hit));
            Assert.Equal("eleven", hit);

            Assert.False(window.TryGetAt(9, out _));
            Assert.False(window.TryGetAt(13, out _));
        }
    }
}
