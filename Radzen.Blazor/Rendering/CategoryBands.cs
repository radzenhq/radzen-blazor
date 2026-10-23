using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// The size and position of one slot in the band a category reserves for its columns or bars.
    /// </summary>
    internal readonly struct BandSlot
    {
        /// <summary>
        /// The width (columns) or height (bars) of the item drawn in this slot.
        /// </summary>
        public double Size { get; }

        /// <summary>
        /// The total size of all slots and the margins between them, centered on the category.
        /// </summary>
        public double Group { get; }

        /// <summary>
        /// The margin between neighboring slots.
        /// </summary>
        public double Margin { get; }

        /// <summary>
        /// The zero-based position of this slot within the group, or -1 when the series does not take part in the band.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// The number of slots in the group.
        /// </summary>
        public int Count { get; }

        public BandSlot(double size, double group, double margin, int index, int count)
        {
            Size = size;
            Group = group;
            Margin = margin;
            Index = index;
            Count = count;
        }

        /// <summary>
        /// Returns the start coordinate (left edge for columns, top edge for bars) of this slot for a category at the given scaled position.
        /// </summary>
        public double Start(double category) => category - Group / 2 + Index * (Size + Margin);

        /// <summary>
        /// Returns the center coordinate of this slot for a category at the given scaled position.
        /// </summary>
        public double Center(double category) => Start(category) + Size / 2;
    }

    /// <summary>
    /// Lays out every column or bar series of a chart in a shared band per category. Each plain series takes its own slot;
    /// stacked series share one slot per stack group so the stack sits side by side with the other slots instead of on top of them.
    /// </summary>
    internal static class CategoryBands
    {
        private readonly record struct StackKey(bool Full, string? Group);

        private static string? NormalizeGroup(string? group) => string.IsNullOrEmpty(group) ? null : group;

        internal static object ColumnSlotKey(IChartSeries series) => series switch
        {
            IChartFullStackedColumnSeries full => new StackKey(true, NormalizeGroup(full.StackGroup)),
            IChartStackedColumnSeries stacked => new StackKey(false, NormalizeGroup(stacked.StackGroup)),
            _ => series,
        };

        internal static object BarSlotKey(IChartSeries series) => series switch
        {
            IChartFullStackedBarSeries full => new StackKey(true, NormalizeGroup(full.StackGroup)),
            IChartStackedBarSeries stacked => new StackKey(false, NormalizeGroup(stacked.StackGroup)),
            _ => series,
        };

        internal static bool SharesColumnSlot(IChartSeries series, IChartSeries other) => ColumnSlotKey(series).Equals(ColumnSlotKey(other));

        internal static bool SharesBarSlot(IChartSeries series, IChartSeries other) => BarSlotKey(series).Equals(BarSlotKey(other));

        internal static bool IsColumn(IChartSeries series) => series is IChartColumnSeries || series is IChartStackedColumnSeries;

        internal static bool IsBar(IChartSeries series) => series is IChartBarSeries;

        private static int ColumnCount(IChartSeries series) => series switch
        {
            IChartColumnSeries column => column.Count,
            IChartStackedColumnSeries stacked => stacked.Count,
            _ => 0,
        };

        private static int BarCount(IChartSeries series) => series is IChartBarSeries bar ? bar.Count : 0;

        private static (int Count, int Index, int MaxItems) Slots(IEnumerable<IChartSeries> series, IChartSeries current, Func<IChartSeries, object> key, Func<IChartSeries, int> items)
        {
            var keys = new List<object>();
            var maxItems = 0;

            foreach (var s in series)
            {
                var k = key(s);

                if (!keys.Contains(k))
                {
                    keys.Add(k);
                }

                maxItems = Math.Max(maxItems, items(s));
            }

            return (keys.Count, keys.IndexOf(key(current)), maxItems);
        }

        /// <summary>
        /// Resolves the slot of a column-type series within the band its chart reserves per category.
        /// </summary>
        internal static BandSlot Columns(RadzenChart chart, IChartSeries series)
        {
            var visible = chart.Series.Where(s => s.Visible && IsColumn(s));
            var (count, index, maxItems) = Slots(visible, series, ColumnSlotKey, ColumnCount);
            var options = chart.ColumnOptions;
            var margin = options.Margin;
            double band;

            if (options.Width is double width)
            {
                band = width * count + margin * (count - 1);
            }
            else if (options.CategoryGap is double gap)
            {
                var step = Math.Abs(chart.CategoryScale.Scale(1, true) - chart.CategoryScale.Scale(0, true));
                band = step * (1 - gap);
            }
            else
            {
                var available = chart.CategoryScale.OutputSize - chart.CategoryAxis.Padding * 2;
                band = available / (maxItems + 2);
            }

            var (size, group) = BandLayout.Resolve(band, count, margin, options.EffectiveMaxWidth);

            return new BandSlot(size, group, margin, index, count);
        }

        /// <summary>
        /// Resolves the slot of a bar-type series within the band its chart reserves per category.
        /// </summary>
        internal static BandSlot Bars(RadzenChart chart, IChartSeries series)
        {
            var visible = chart.Series.Where(s => s.Visible && IsBar(s));
            var (count, index, maxItems) = Slots(visible, series, BarSlotKey, BarCount);
            var options = chart.BarOptions;
            var margin = options.Margin;
            double band;

            if (options.Height is double height)
            {
                band = height * count + margin * (count - 1);
            }
            else if (options.CategoryGap is double gap)
            {
                var step = Math.Abs(chart.ValueScale.Scale(1, true) - chart.ValueScale.Scale(0, true));
                band = step * (1 - gap);
            }
            else
            {
                band = chart.ValueScale.OutputSize / (maxItems + 2);
            }

            var (size, group) = BandLayout.Resolve(band, count, margin, options.EffectiveMaxHeight);

            return new BandSlot(size, group, margin, index, count);
        }
    }
}
