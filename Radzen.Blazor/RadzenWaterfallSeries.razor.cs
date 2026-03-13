using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// A chart series that displays data as a waterfall chart in a RadzenChart.
    /// Each column starts where the previous one ended, showing how sequential values contribute to a running total.
    /// Items with the summary property set to <c>true</c> render as total bars starting from zero.
    /// Positive values are colored with <see cref="PositiveFill"/>, negative with <see cref="NegativeFill"/>,
    /// and summary bars with <see cref="SummaryFill"/>.
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenChart&gt;
    ///     &lt;RadzenWaterfallSeries Data=@data CategoryProperty="Label" ValueProperty="Amount"
    ///         SummaryProperty="IsTotal" Title="Cash Flow" /&gt;
    /// &lt;/RadzenChart&gt;
    /// </code>
    /// </example>
    public partial class RadzenWaterfallSeries<TItem> : CartesianSeries<TItem>, IChartColumnSeries
    {
        /// <summary>
        /// Gets or sets the fill color for positive value columns.
        /// When not set, the theme color scheme applies.
        /// </summary>
        [Parameter]
        public string? PositiveFill { get; set; }

        /// <summary>
        /// Gets or sets the fill color for negative value columns.
        /// When not set, the theme color scheme applies.
        /// </summary>
        [Parameter]
        public string? NegativeFill { get; set; }

        /// <summary>
        /// Gets or sets the fill color for summary (total) columns.
        /// When not set, the theme color scheme applies.
        /// </summary>
        [Parameter]
        public string? SummaryFill { get; set; }

        /// <summary>
        /// Gets or sets the stroke (border) color for all columns.
        /// </summary>
        [Parameter]
        public string? Stroke { get; set; }

        /// <summary>
        /// Gets or sets the width of the column border in pixels.
        /// </summary>
        [Parameter]
        public double StrokeWidth { get; set; }

        /// <summary>
        /// Gets or sets the line type for column borders.
        /// </summary>
        [Parameter]
        public LineType LineType { get; set; }

        /// <summary>
        /// Gets or sets the name of a boolean property on <typeparamref name="TItem"/> that indicates summary (total) items.
        /// Summary items render as bars starting from zero, representing the cumulative total up to that point.
        /// </summary>
        [Parameter]
        public string? SummaryProperty { get; set; }

        /// <inheritdoc />
        public override string Color => PositiveFill ?? string.Empty;

        int IChartColumnSeries.Count => Items?.Count ?? 0;

        internal Func<TItem, bool>? IsSummary
        {
            get
            {
                if (string.IsNullOrEmpty(SummaryProperty))
                {
                    return null;
                }

                return PropertyAccess.Getter<TItem, bool>(SummaryProperty);
            }
        }

        /// <inheritdoc />
        public override ScaleBase TransformValueScale(ScaleBase scale)
        {
            ArgumentNullException.ThrowIfNull(scale);

            if (Items == null || !Items.Any())
            {
                return scale;
            }

            var isSummary = IsSummary;
            double runningTotal = 0;
            double minValue = 0;
            double maxValue = 0;

            foreach (var item in Items)
            {
                var val = Value(item);
                var summary = isSummary?.Invoke(item) ?? false;

                if (summary)
                {
                    minValue = Math.Min(minValue, Math.Min(0, runningTotal));
                    maxValue = Math.Max(maxValue, Math.Max(0, runningTotal));
                }
                else
                {
                    var prevTotal = runningTotal;
                    runningTotal += val;
                    minValue = Math.Min(minValue, Math.Min(prevTotal, runningTotal));
                    maxValue = Math.Max(maxValue, Math.Max(prevTotal, runningTotal));
                }
            }

            scale.Input.MergeWidth(new ScaleRange { Start = minValue, End = maxValue });
            return scale;
        }

        internal IList<WaterfallItem> ComputeWaterfallItems()
        {
            var result = new List<WaterfallItem>();
            var isSummary = IsSummary;
            double runningTotal = 0;

            foreach (var item in Items)
            {
                var val = Value(item);
                var summary = isSummary?.Invoke(item) ?? false;

                if (summary)
                {
                    result.Add(new WaterfallItem { Start = 0, End = runningTotal, IsSummary = true });
                }
                else
                {
                    var start = runningTotal;
                    runningTotal += val;
                    result.Add(new WaterfallItem { Start = start, End = runningTotal, IsSummary = false });
                }
            }

            return result;
        }

        internal class WaterfallItem
        {
            public double Start { get; set; }
            public double End { get; set; }
            public bool IsSummary { get; set; }
        }

        private IList<IChartSeries> ColumnSeries =>
            RequireChart().Series.Where(s => s is IChartColumnSeries).Cast<IChartSeries>().ToList();

        private IList<IChartSeries> VisibleColumnSeries =>
            ColumnSeries.Where(s => s.Visible).ToList();

        internal double BandWidth
        {
            get
            {
                var columnSeries = VisibleColumnSeries;
                var chart = RequireChart();

                if (chart.ColumnOptions.Width.HasValue)
                {
                    return chart.ColumnOptions.Width.Value * columnSeries.Count + chart.ColumnOptions.Margin * (columnSeries.Count - 1);
                }

                var availableWidth = chart.CategoryScale.OutputSize - (chart.CategoryAxis.Padding * 2);
                var bands = columnSeries.Cast<IChartColumnSeries>().Max(s => s.Count) + 2;
                return availableWidth / bands;
            }
        }

        /// <inheritdoc />
        protected override string TooltipStyle(TItem item)
        {
            var style = base.TooltipStyle(item);
            var index = Items.IndexOf(item);
            if (index < 0) return style;

            var wfItems = ComputeWaterfallItems();
            var wf = wfItems[index];
            var color = wf.IsSummary ? SummaryFill : (wf.End >= wf.Start ? PositiveFill : NegativeFill);

            if (color != null)
            {
                return $"{style}; border-color: {color};";
            }

            return style;
        }

        /// <inheritdoc />
        internal override double TooltipX(TItem item)
        {
            var chart = RequireChart();
            var columnSeries = VisibleColumnSeries;
            var seriesIndex = columnSeries.IndexOf(this);
            var padding = chart.ColumnOptions.Margin;
            var bandWidth = BandWidth;
            var width = bandWidth / columnSeries.Count - padding + padding / columnSeries.Count;
            var category = ComposeCategory(chart.CategoryScale);
            var x = category(item) - bandWidth / 2 + seriesIndex * width + seriesIndex * padding;

            return x + width / 2;
        }

        /// <inheritdoc />
        internal override double TooltipY(TItem item)
        {
            var chart = RequireChart();
            var index = Items.IndexOf(item);
            if (index < 0) return 0;

            var wfItems = ComputeWaterfallItems();
            var wf = wfItems[index];
            return chart.ValueScale.Scale(wf.End, true);
        }

        /// <inheritdoc />
        public override bool Contains(double x, double y, double tolerance)
        {
            return DataAt(x, y).Item1 != null;
        }

        /// <inheritdoc />
        public override (object, Point) DataAt(double x, double y)
        {
            var chart = Chart;
            if (chart == null) return (default!, new Point());

            var category = ComposeCategory(chart.CategoryScale);
            var columnSeries = VisibleColumnSeries;
            var seriesIndex = columnSeries.IndexOf(this);
            var padding = chart.ColumnOptions.Margin;
            var bandWidth = BandWidth;
            var width = chart.ColumnOptions.Width ?? bandWidth / columnSeries.Count - padding + padding / columnSeries.Count;

            var wfItems = ComputeWaterfallItems();

            for (var i = 0; i < Items.Count; i++)
            {
                var data = Items[i];
                var wf = wfItems[i];

                var startX = category(data) - bandWidth / 2 + seriesIndex * width + seriesIndex * padding;
                var endX = startX + width;
                var y1 = chart.ValueScale.Scale(wf.Start, true);
                var y2 = chart.ValueScale.Scale(wf.End, true);
                var startY = Math.Min(y1, y2);
                var endY = Math.Max(y1, y2);

                if (startX <= x && x <= endX && startY <= y && y <= endY)
                {
                    return (data!, new Point { X = x, Y = y });
                }
            }

            return (default!, new Point());
        }
    }
}
