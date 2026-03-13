using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A chart series that displays data as horizontal range bars in a RadzenChart.
    /// Each bar spans from a minimum value to a maximum value, useful for showing value ranges per category.
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenChart&gt;
    ///     &lt;RadzenRangeBarSeries Data=@data CategoryProperty="Task"
    ///         MinProperty="Start" MaxProperty="End" Title="Timeline" /&gt;
    /// &lt;/RadzenChart&gt;
    /// </code>
    /// </example>
    public partial class RadzenRangeBarSeries<TItem> : CartesianSeries<TItem>, IChartBarSeries
    {
        /// <summary>
        /// Gets or sets the name of the property that provides the minimum (left) value.
        /// </summary>
        [Parameter]
        public string? MinProperty { get; set; }

        /// <summary>
        /// Gets or sets the name of the property that provides the maximum (right) value.
        /// </summary>
        [Parameter]
        public string? MaxProperty { get; set; }

        /// <summary>
        /// Gets or sets the fill color of the bars.
        /// </summary>
        [Parameter]
        public string? Fill { get; set; }

        /// <summary>
        /// Gets or sets a collection of fill colors for individual bars.
        /// </summary>
        [Parameter]
        public IEnumerable<string>? Fills { get; set; }

        /// <summary>
        /// Gets or sets the stroke (border) color of the bars.
        /// </summary>
        [Parameter]
        public string? Stroke { get; set; }

        /// <summary>
        /// Gets or sets a collection of stroke colors for individual bars.
        /// </summary>
        [Parameter]
        public IEnumerable<string>? Strokes { get; set; }

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

        /// <inheritdoc />
        public override string Color => Fill ?? string.Empty;

        int IChartBarSeries.Count => Items?.Count ?? 0;

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.TryGetValue<string>(nameof(MaxProperty), out var max) && max != MaxProperty)
            {
                ValueProperty = max;
            }

            await base.SetParametersAsync(parameters);
        }

        internal Func<TItem, double> Min
        {
            get
            {
                if (string.IsNullOrEmpty(MinProperty))
                {
                    throw new ArgumentException("MinProperty should not be empty");
                }

                return PropertyAccess.Getter<TItem, double>(MinProperty);
            }
        }

        internal Func<TItem, double> Max
        {
            get
            {
                if (string.IsNullOrEmpty(MaxProperty))
                {
                    throw new ArgumentException("MaxProperty should not be empty");
                }

                return PropertyAccess.Getter<TItem, double>(MaxProperty);
            }
        }

        /// <inheritdoc />
        public override ScaleBase TransformCategoryScale(ScaleBase scale)
        {
            ArgumentNullException.ThrowIfNull(scale);

            if (Items != null && Items.Any())
            {
                var min = Min;
                var max = Max;

                var minValue = Items.Min(item => min(item));
                var maxValue = Items.Max(item => max(item));

                scale.Input.MergeWidth(new ScaleRange { Start = minValue, End = maxValue });
            }

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

        private IList<IChartSeries> BarSeries =>
            RequireChart().Series.Where(s => s is IChartBarSeries).Cast<IChartSeries>().ToList();

        private IList<IChartSeries> VisibleBarSeries =>
            BarSeries.Where(s => s.Visible).ToList();

        private double BandHeight
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

            if (index >= 0)
            {
                var color = PickColor(index, Fills, Fill);
                if (color != null)
                {
                    style = $"{style}; border-color: {color};";
                }
            }

            return style;
        }

        /// <inheritdoc />
        protected override string TooltipValue(TItem item)
        {
            var chart = RequireChart();
            var minVal = chart.ValueAxis.Format(chart.CategoryScale, chart.CategoryScale.Value(Min(item)));
            var maxVal = chart.ValueAxis.Format(chart.CategoryScale, chart.CategoryScale.Value(Max(item)));
            return $"{minVal} - {maxVal}";
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
            return chart.CategoryScale.Scale(Max(item), true);
        }

        /// <inheritdoc />
        internal override double TooltipY(TItem item)
        {
            var chart = RequireChart();
            var category = ComposeCategory(chart.ValueScale);
            var barSeries = VisibleBarSeries;
            var index = barSeries.IndexOf(this);
            if (barSeries.Count == 0 || index < 0) return 0;

            var padding = chart.BarOptions?.Margin ?? 0;
            var bandHeight = BandHeight;
            var height = bandHeight / barSeries.Count - padding + padding / barSeries.Count;
            var y = category(item) - bandHeight / 2 + index * height + index * padding;

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
            var index = barSeries.IndexOf(this);
            if (barSeries.Count == 0 || index < 0) return (default!, new Point());

            var padding = chart.BarOptions?.Margin ?? 0;
            var bandHeight = BandHeight;
            var height = bandHeight / barSeries.Count - padding + padding / barSeries.Count;

            foreach (var data in Items)
            {
                var startY = category(data) - bandHeight / 2 + index * height + index * padding;
                var endY = startY + height;
                var minX = chart.CategoryScale.Scale(Min(data), true);
                var maxX = chart.CategoryScale.Scale(Max(data), true);
                var sX = Math.Min(minX, maxX);
                var eX = Math.Max(minX, maxX);

                if (sX <= x && x <= eX && startY <= y && y <= endY)
                {
                    return (data!, new Point { X = x, Y = y });
                }
            }

            return (default!, new Point());
        }
    }
}
