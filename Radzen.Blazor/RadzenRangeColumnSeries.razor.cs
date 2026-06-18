using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A chart series that displays data as vertical range columns in a RadzenChart.
    /// Each column spans from a minimum value to a maximum value, useful for showing value ranges per category.
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenChart&gt;
    ///     &lt;RadzenRangeColumnSeries Data=@data CategoryProperty="Month"
    ///         MinProperty="Low" MaxProperty="High" Title="Temperature Range" /&gt;
    /// &lt;/RadzenChart&gt;
    /// </code>
    /// </example>
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2026, Justification = TrimMessages.DataTypePreserved)]
    public partial class RadzenRangeColumnSeries<TItem> : CartesianSeries<TItem>, IChartColumnSeries
    {
        /// <summary>
        /// Gets or sets the name of the property that provides the minimum (bottom) value.
        /// </summary>
        [Parameter]
        public string? MinProperty { get; set; }

        /// <summary>
        /// Gets or sets the name of the property that provides the maximum (top) value.
        /// </summary>
        [Parameter]
        public string? MaxProperty { get; set; }

        /// <summary>
        /// Gets or sets the fill color of the columns.
        /// </summary>
        [Parameter]
        public string? Fill { get; set; }

        /// <summary>
        /// Gets or sets a collection of fill colors for individual columns.
        /// </summary>
        [Parameter]
        public IEnumerable<string>? Fills { get; set; }

        /// <summary>
        /// Gets or sets the stroke (border) color of the columns.
        /// </summary>
        [Parameter]
        public string? Stroke { get; set; }

        /// <summary>
        /// Gets or sets a collection of stroke colors for individual columns.
        /// </summary>
        [Parameter]
        public IEnumerable<string>? Strokes { get; set; }

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
        /// Specifies how the series is filled. Set to <see cref="FillMode.Solid"/> by default.
        /// Use <see cref="FillMode.Gradient"/> for a fill that fades toward the axis baseline, or <see cref="FillMode.None"/> to render only the outline.
        /// </summary>
        /// <value>The fill mode. Default is <see cref="FillMode.Solid"/>.</value>
        [Parameter]
        public FillMode FillMode { get; set; } = FillMode.Solid;

        /// <summary>
        /// Specifies the opacity at the value end of the gradient fill. Used when <see cref="FillMode"/> is <see cref="FillMode.Gradient"/>.
        /// </summary>
        /// <value>The gradient start opacity. Default is <c>0.85</c>.</value>
        [Parameter]
        public double GradientStartOpacity { get; set; } = 0.85;

        /// <summary>
        /// Specifies the opacity at the baseline of the gradient fill. Used when <see cref="FillMode"/> is <see cref="FillMode.Gradient"/>.
        /// </summary>
        /// <value>The gradient end opacity. Default is <c>0.4</c>.</value>
        [Parameter]
        public double GradientEndOpacity { get; set; } = 0.4;

        /// <inheritdoc />
        public override string Color => Fill ?? string.Empty;

        int IChartColumnSeries.Count => Items?.Count ?? 0;

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
        public override ScaleBase TransformValueScale(ScaleBase scale)
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

        private IList<IChartSeries> ColumnSeries =>
            RequireChart().Series.Where(s => s is IChartColumnSeries).Cast<IChartSeries>().ToList();

        private IList<IChartSeries> VisibleColumnSeries =>
            ColumnSeries.Where(s => s.Visible).ToList();

        private double BandWidth
        {
            get
            {
                var columnSeries = VisibleColumnSeries;
                var chart = RequireChart();

                if (chart.ColumnOptions.Width.HasValue)
                {
                    return chart.ColumnOptions.Width.Value * columnSeries.Count + chart.ColumnOptions.Margin * (columnSeries.Count - 1);
                }

                if (chart.ColumnOptions.CategoryGap is double gap)
                {
                    var step = System.Math.Abs(chart.CategoryScale.Scale(1, true) - chart.CategoryScale.Scale(0, true));
                    return step * (1 - gap);
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
            var minVal = chart.ValueAxis.Format(chart.ValueScale, chart.ValueScale.Value(Min(item)));
            var maxVal = chart.ValueAxis.Format(chart.ValueScale, chart.ValueScale.Value(Max(item)));
            return $"{minVal} - {maxVal}";
        }

        /// <inheritdoc />
        internal override double TooltipX(TItem item)
        {
            var chart = RequireChart();
            var columnSeries = VisibleColumnSeries;
            var index = columnSeries.IndexOf(this);
            var padding = chart.ColumnOptions.Margin;
            var bandWidth = BandWidth;
            var (width, groupWidth) = Rendering.BandLayout.Resolve(bandWidth, columnSeries.Count, padding, chart.ColumnOptions.EffectiveMaxWidth);
            var category = ComposeCategory(chart.CategoryScale);
            var x = category(item) - groupWidth / 2 + index * (width + padding);

            return x + width / 2;
        }

        /// <inheritdoc />
        internal override double TooltipY(TItem item)
        {
            var chart = RequireChart();
            return chart.ValueScale.Scale(Max(item), true);
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
            if (chart == null)
            {
                return (default!, new Point());
            }

            var category = ComposeCategory(chart.CategoryScale);
            var columnSeries = VisibleColumnSeries;
            var index = columnSeries.IndexOf(this);
            var padding = chart.ColumnOptions.Margin;
            var bandWidth = BandWidth;
            var (width, groupWidth) = Rendering.BandLayout.Resolve(bandWidth, columnSeries.Count, padding, chart.ColumnOptions.EffectiveMaxWidth);

            foreach (var data in Items)
            {
                var startX = category(data) - groupWidth / 2 + index * (width + padding);
                var endX = startX + width;
                var minY = chart.ValueScale.Scale(Max(data), true);
                var maxY = chart.ValueScale.Scale(Min(data), true);
                var startY = Math.Min(minY, maxY);
                var endY = Math.Max(minY, maxY);

                if (startX <= x && x <= endX && startY <= y && y <= endY)
                {
                    return (data!, new Point { X = x, Y = y });
                }
            }

            return (default!, new Point());
        }
    }
}
