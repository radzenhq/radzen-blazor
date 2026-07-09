using System;
using System.Collections.Generic;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class GanttHistogramLayoutTests
    {
        private static (DateTime, DateTime) Day(int day) => (new DateTime(2026, 1, day), new DateTime(2026, 1, day + 1));

        [Fact]
        public void Compute_EmptyColumns_ReturnsEmpty()
        {
            var values = GanttHistogramLayout.Compute(
                new List<(DateTime, DateTime)>(),
                new List<(DateTime, DateTime, double)> { (new DateTime(2026, 1, 1), new DateTime(2026, 1, 5), 1) });

            Assert.Empty(values);
        }

        [Fact]
        public void Compute_NoItems_ReturnsZeros()
        {
            var values = GanttHistogramLayout.Compute(
                new List<(DateTime, DateTime)> { Day(1), Day(2) },
                new List<(DateTime, DateTime, double)>());

            Assert.Equal(new double[] { 0, 0 }, values);
        }

        [Fact]
        public void Compute_CountsOverlappingItemsPerColumn()
        {
            var columns = new List<(DateTime, DateTime)> { Day(1), Day(2), Day(3), Day(4) };
            var items = new List<(DateTime, DateTime, double)>
            {
                (new DateTime(2026, 1, 1), new DateTime(2026, 1, 3), 1),
                (new DateTime(2026, 1, 2), new DateTime(2026, 1, 5), 1)
            };

            var values = GanttHistogramLayout.Compute(columns, items);

            Assert.Equal(new double[] { 1, 2, 1, 1 }, values);
        }

        [Fact]
        public void Compute_ItemEndIsExclusive()
        {
            var columns = new List<(DateTime, DateTime)> { Day(1), Day(2) };
            var items = new List<(DateTime, DateTime, double)>
            {
                (new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), 1)
            };

            var values = GanttHistogramLayout.Compute(columns, items);

            Assert.Equal(new double[] { 1, 0 }, values);
        }

        [Fact]
        public void Compute_SumsCustomValues()
        {
            var columns = new List<(DateTime, DateTime)> { Day(1) };
            var items = new List<(DateTime, DateTime, double)>
            {
                (new DateTime(2026, 1, 1), new DateTime(2026, 1, 3), 2.5),
                (new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), 4)
            };

            var values = GanttHistogramLayout.Compute(columns, items);

            Assert.Equal(new double[] { 6.5 }, values);
        }

        [Fact]
        public void Compute_MilestoneContributesToContainingColumn()
        {
            var columns = new List<(DateTime, DateTime)> { Day(1), Day(2), Day(3) };
            var milestone = new DateTime(2026, 1, 2);
            var items = new List<(DateTime, DateTime, double)>
            {
                (milestone, milestone, 1)
            };

            var values = GanttHistogramLayout.Compute(columns, items);

            Assert.Equal(new double[] { 0, 1, 0 }, values);
        }

        [Fact]
        public void Compute_ItemOutsideRange_ContributesNothing()
        {
            var columns = new List<(DateTime, DateTime)> { Day(1), Day(2) };
            var items = new List<(DateTime, DateTime, double)>
            {
                (new DateTime(2026, 2, 1), new DateTime(2026, 2, 5), 1)
            };

            var values = GanttHistogramLayout.Compute(columns, items);

            Assert.Equal(new double[] { 0, 0 }, values);
        }
    }
}
