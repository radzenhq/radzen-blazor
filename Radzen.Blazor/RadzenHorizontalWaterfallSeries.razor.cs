using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// A chart series that displays data as a horizontal waterfall chart in a RadzenChart.
    /// Each bar starts where the previous one ended, showing how sequential values contribute to a running total.
    /// Items with the summary property set to <c>true</c> render as total bars starting from zero.
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenChart&gt;
    ///     &lt;RadzenHorizontalWaterfallSeries Data=@data CategoryProperty="Label" ValueProperty="Amount"
    ///         SummaryProperty="IsTotal" Title="Cash Flow" /&gt;
    /// &lt;/RadzenChart&gt;
    /// </code>
    /// </example>
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2026, Justification = TrimMessages.DataTypePreserved)]
    public partial class RadzenHorizontalWaterfallSeries<TItem> : CartesianSeries<TItem>, IChartBarSeries
    {
        /// <summary>
        /// Gets or sets the fill color for positive value bars.
        /// When not set, the theme color scheme applies.
        /// </summary>
        [Parameter]
        public string? PositiveFill { get; set; }

        /// <summary>
        /// Gets or sets the fill color for negative value bars.
        /// When not set, the theme color scheme applies.
        /// </summary>
        [Parameter]
        public string? NegativeFill { get; set; }

        /// <summary>
        /// Gets or sets the fill color for summary (total) bars.
        /// When not set, the theme color scheme applies.
        /// </summary>
        [Parameter]
        public string? SummaryFill { get; set; }

        /// <summary>
        /// Gets or sets the stroke (border) color for all bars.
        /// </summary>
        [Parameter]
        public string? Stroke { get; set; }

        /// <summary>
        /// Gets or sets the width of the bar border in pixels.
        /// </summary>
        [Parameter]
        public double StrokeWidth { get; set; }

        /// <summary>
        /// Gets or sets the line type for bar borders.
        /// </summary>
        [Parameter]
        public LineType LineType { get; set; }

        /// <summary>
        /// Gets or sets the name of a boolean property on <typeparamref name="TItem"/> that indicates summary (total) items.
        /// </summary>
        [Parameter]
        public string? SummaryProperty { get; set; }

        /// <inheritdoc />
        public override string Color => PositiveFill ?? string.Empty;

        int IChartBarSeries.Count => Items?.Count ?? 0;

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
        public override ScaleBase TransformCategoryScale(ScaleBase scale)
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

        /// <inheritdoc />
        public override ScaleBase TransformValueScale(ScaleBase scale)
        {
            return base.TransformCategoryScale(scale);
        }

        /// <inheritdoc />
        protected override IList<object> GetCategories()
        {
            return base.GetCategories().Reverse().ToList();
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

        private IList<IChartSeries> BarSeries =>
            RequireChart().Series.Where(s => s is IChartBarSeries).Cast<IChartSeries>().ToList();

        private IList<IChartSeries> VisibleBarSeries =>
            BarSeries.Where(s => s.Visible).ToList();

        internal double BandHeight
        {
            get
            {
                var barSeries = VisibleBarSeries;
                if (barSeries.Count == 0) return 0;

                var chart = RequireChart();
                var barOptions = chart.BarOptions;

                if (barOptions?.Height.HasValue == true)
                {
                    return barOptions.Height.Value * barSeries.Count;
                }

                var availableHeight = chart.ValueScale.OutputSize;
                var bands = barSeries.Cast<IChartBarSeries>().Max(s => s.Count) + 2;
                return availableHeight / bands;
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
        protected override string TooltipValue(TItem item)
        {
            var chart = RequireChart();
            return chart.ValueAxis.Format(chart.CategoryScale, chart.CategoryScale.Value(Value(item)));
        }

        /// <inheritdoc />
        protected override string TooltipTitle(TItem item)
        {
            var chart = RequireChart();
            var category = Category(chart.ValueScale);
            return chart.CategoryAxis.Format(chart.ValueScale, chart.ValueScale.Value(category(item)));
        }

        /// <inheritdoc />
        internal override double TooltipX(TItem item)
        {
            var chart = RequireChart();
            var index = Items.IndexOf(item);
            if (index < 0) return 0;

            var wfItems = ComputeWaterfallItems();
            var wf = wfItems[index];
            return chart.CategoryScale.Scale(wf.End, true);
        }

        /// <inheritdoc />
        internal override double TooltipY(TItem item)
        {
            var chart = RequireChart();
            var category = ComposeCategory(chart.ValueScale);
            var barSeries = VisibleBarSeries;
            var seriesIndex = barSeries.IndexOf(this);
            if (barSeries.Count == 0 || seriesIndex < 0) return 0;

            var padding = chart.BarOptions?.Margin ?? 0;
            var bandHeight = BandHeight;
            var height = bandHeight / barSeries.Count - padding + padding / barSeries.Count;
            var y = category(item) - bandHeight / 2 + seriesIndex * height + seriesIndex * padding;

            return y + height / 2;
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

            var category = ComposeCategory(chart.ValueScale);
            var barSeries = VisibleBarSeries;
            var seriesIndex = barSeries.IndexOf(this);
            if (barSeries.Count == 0 || seriesIndex < 0) return (default!, new Point());

            var padding = chart.BarOptions?.Margin ?? 0;
            var bandHeight = BandHeight;
            var height = bandHeight / barSeries.Count - padding + padding / barSeries.Count;

            var wfItems = ComputeWaterfallItems();

            for (var i = 0; i < Items.Count; i++)
            {
                var data = Items[i];
                var wf = wfItems[i];

                var startY = category(data) - bandHeight / 2 + seriesIndex * height + seriesIndex * padding;
                var endY = startY + height;
                var x1 = chart.CategoryScale.Scale(wf.Start, true);
                var x2 = chart.CategoryScale.Scale(wf.End, true);
                var sX = Math.Min(x1, x2);
                var eX = Math.Max(x1, x2);

                if (sX <= x && x <= eX && startY <= y && y <= endY)
                {
                    return (data!, new Point { X = x, Y = y });
                }
            }

            return (default!, new Point());
        }
    }
}
