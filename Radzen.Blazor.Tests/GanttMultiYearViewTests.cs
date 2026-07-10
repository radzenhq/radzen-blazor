using System;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class GanttMultiYearViewTests
    {
        [Fact]
        public void BuildQuarterColumns_SpansYearBoundary()
        {
            var columns = RadzenGanttYearsView<object>.BuildQuarterColumns(
                new DateTime(2025, 11, 15), new DateTime(2026, 5, 15), 80, CultureInfo.InvariantCulture, "Q{0}");

            Assert.Equal(3, columns.Count);
            Assert.Equal(new DateTime(2025, 10, 1), columns[0].Start);
            Assert.Equal("Q4", columns[0].Label);
            Assert.Equal("2025", columns[0].GroupLabel);
            Assert.Equal("Q1", columns[1].Label);
            Assert.Equal("2026", columns[1].GroupLabel);
            Assert.Equal("Q2", columns[2].Label);
            Assert.Equal(new DateTime(2026, 7, 1), columns[2].End);
        }

        [Fact]
        public void BuildQuarterColumns_UsesQuarterFormat()
        {
            var columns = RadzenGanttYearsView<object>.BuildQuarterColumns(
                new DateTime(2025, 1, 1), new DateTime(2025, 4, 1), 80, CultureInfo.InvariantCulture, "T{0}");

            Assert.Equal("T1", columns[0].Label);
        }

        [Fact]
        public void BuildQuarterColumns_WidthsAreProportionalToDays()
        {
            var columns = RadzenGanttYearsView<object>.BuildQuarterColumns(
                new DateTime(2025, 1, 1), new DateTime(2026, 1, 1), 80, CultureInfo.InvariantCulture, "Q{0}");

            Assert.Equal(4, columns.Count);
            Assert.True(columns[2].WidthPx > columns[0].WidthPx);

            var totalWidth = columns.Sum(c => c.WidthPx);
            Assert.InRange(totalWidth, 315, 325);
        }

        [Fact]
        public void BuildMonthColumns_SpansMultipleYears()
        {
            var columns = RadzenGanttYearView<object>.BuildMonthColumns(
                new DateTime(2025, 1, 1), new DateTime(2027, 1, 1), 80, CultureInfo.InvariantCulture, "Q{0}");

            Assert.Equal(24, columns.Count);
            Assert.Equal("Q1 2025", columns[0].GroupLabel);
            Assert.Equal("Q4 2026", columns[23].GroupLabel);
            Assert.Equal(new DateTime(2027, 1, 1), columns[23].End);
        }

        [Fact]
        public void BuildMonthColumns_MonthCellsAreApproximatelyMonthWidth()
        {
            var columns = RadzenGanttYearView<object>.BuildMonthColumns(
                new DateTime(2025, 1, 1), new DateTime(2026, 1, 1), 80, CultureInfo.InvariantCulture, "Q{0}");

            Assert.Equal(12, columns.Count);
            foreach (var column in columns)
            {
                Assert.InRange(column.WidthPx, 70, 90);
            }

            var totalWidth = columns.Sum(c => c.WidthPx);
            Assert.InRange(totalWidth, 950, 970);
        }
    }
}
