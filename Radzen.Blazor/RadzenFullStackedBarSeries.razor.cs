using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// Renders 100% stacked bar series in <see cref="RadzenChart" />.
    /// Each category's values are normalized so they total 100%.
    /// </summary>
    /// <typeparam name="TItem">The type of the series data item.</typeparam>
    public partial class RadzenFullStackedBarSeries<TItem> : CartesianSeries<TItem>, IChartFullStackedBarSeries
    {
        /// <summary>
        /// Specifies the fill (background color) of the bar series.
        /// </summary>
        /// <value>The fill.</value>
        [Parameter]
        public string? Fill { get; set; }

        /// <summary>
        /// Specifies a list of colors that will be used to set the individual bar backgrounds.
        /// </summary>
        /// <value>The fills.</value>
        [Parameter]
        public IEnumerable<string>? Fills { get; set; }

        /// <summary>
        /// Specifies the stroke (border color) of the bar series.
        /// </summary>
        /// <value>The stroke.</value>
        [Parameter]
        public string? Stroke { get; set; }

        /// <summary>
        /// Specifies a list of colors that will be used to set the individual bar borders.
        /// </summary>
        /// <value>The strokes.</value>
        [Parameter]
        public IEnumerable<string>? Strokes { get; set; }

        /// <summary>
        /// Gets or sets the width of the stroke (border).
        /// </summary>
        /// <value>The width of the stroke.</value>
        [Parameter]
        public double StrokeWidth { get; set; }

        /// <summary>
        /// Gets or sets the type of the line used to render the bar border.
        /// </summary>
        /// <value>The type of the line.</value>
        [Parameter]
        public LineType LineType { get; set; }

        /// <summary>
        /// Gets or sets the color range of the fill.
        /// </summary>
        /// <value>The color range of the fill.</value>
        [Parameter]
        public IList<SeriesColorRange>? FillRange { get; set; }

        /// <summary>
        /// Gets or sets the color range of the stroke.
        /// </summary>
        /// <value>The color range of the stroke.</value>
        [Parameter]
        public IList<SeriesColorRange>? StrokeRange { get; set; }

        /// <inheritdoc />
        public override string Color
        {
            get
            {
                return Fill ?? string.Empty;
            }
        }

        /// <inheritdoc />
        public override ScaleBase TransformCategoryScale(ScaleBase scale)
        {
            ArgumentNullException.ThrowIfNull(scale);

            if (Items.Any())
            {
                scale.Input.MergeWidth(new ScaleRange { Start = 0, End = 100 });
            }

            return scale;
        }

        /// <inheritdoc />
        public override ScaleBase TransformValueScale(ScaleBase scale)
        {
            ArgumentNullException.ThrowIfNull(scale);

            return base.TransformCategoryScale(scale);
        }

        /// <inheritdoc />
        protected override IList<object> GetCategories()
        {
            return base.GetCategories().Reverse().ToList();
        }

        private IList<IChartSeries> BarSeries => Chart?.Series?.Where(series => series is IChartFullStackedBarSeries).Cast<IChartSeries>().ToList() ?? new List<IChartSeries>();

        private IList<IChartSeries> VisibleBarSeries => BarSeries.Where(series => series.Visible).ToList();

        private IList<IChartStackedBarSeries> StackedBarSeries => VisibleBarSeries.Cast<IChartStackedBarSeries>().ToList();

        /// <summary>
        /// Gets the percentage value for an item by normalizing against the sum of absolute values for its category.
        /// </summary>
        private double PercentValue(TItem item)
        {
            if (Chart == null)
            {
                return 0;
            }

            var value = Value(item);
            var category = ComposeCategory(Chart.ValueScale);
            var categoryValue = category(item);
            var stackedBarSeries = StackedBarSeries;

            var totalAbsolute = stackedBarSeries.SelectMany(series => series.ValuesForCategory(categoryValue)).Sum(v => Math.Abs(v));

            if (totalAbsolute == 0)
            {
                return 0;
            }

            return (value / totalAbsolute) * 100;
        }

        /// <inheritdoc />
        protected override string TooltipStyle(TItem item)
        {
            var style = base.TooltipStyle(item);

            var index = Items.IndexOf(item);

            if (index >= 0)
            {
                var color = PickColor(index, Fills, Fill, FillRange, Value(item));

                if (color != null)
                {
                    style = $"{style}; border-color: {color};";
                }
            }

            return style;
        }

        private double BandHeight
        {
            get
            {
                var barSeries = VisibleBarSeries;

                if (Chart?.BarOptions?.Height.HasValue == true)
                {
                    return Chart.BarOptions.Height.Value * barSeries.Count;
                }
                else if (Chart != null)
                {
                    var availableHeight = Chart.ValueScale.OutputSize;
                    var bands = barSeries.Cast<IChartStackedBarSeries>().Max(series => series.Count) + 2;
                    return availableHeight / bands;
                }

                return 0;
            }
        }

        double BarHeight => Chart?.BarOptions?.Height ?? (Chart != null ? BandHeight - Chart.BarOptions.Margin : 0);

        int BarIndex => VisibleBarSeries.IndexOf(this);

        private double GetBarTop(TItem item, Func<TItem, double>? category = null)
        {
            if (Chart == null)
            {
                return 0;
            }

            category = category ?? ComposeCategory(Chart.ValueScale);

            return category(item) - BarHeight / 2;
        }

        private (double positiveSum, double negativeSum) PercentSum(int barIndex, IEnumerable<IChartStackedBarSeries> stackedBarSeries, double category)
        {
            var allValues = stackedBarSeries.SelectMany(series => series.ValuesForCategory(category)).ToList();
            var totalAbsolute = allValues.Sum(v => Math.Abs(v));

            if (totalAbsolute == 0)
            {
                return (0, 0);
            }

            var precedingValues = stackedBarSeries.Take(barIndex).SelectMany(series => series.ValuesForCategory(category))
                .Select(v => (v / totalAbsolute) * 100).DefaultIfEmpty(0).ToList();

            var positiveSum = precedingValues.Where(v => v >= 0).DefaultIfEmpty(0).Sum();
            var negativeSum = precedingValues.Where(v => v < 0).DefaultIfEmpty(0).Sum();
            return (positiveSum, negativeSum);
        }

        private double GetBarRight(TItem item, int barIndex, Func<TItem, double> category, IEnumerable<IChartStackedBarSeries> stackedBarSeries)
        {
            var percentValue = PercentValue(item);
            var (positiveSum, negativeSum) = PercentSum(barIndex, stackedBarSeries, category(item));

            if (Chart == null)
            {
                return 0;
            }

            if (percentValue >= 0)
            {
                return Chart.CategoryScale.Scale(percentValue + positiveSum);
            }
            else
            {
                return Chart.CategoryScale.Scale(negativeSum);
            }
        }

        private double GetBarLeft(TItem item, int barIndex, Func<TItem, double> category, IEnumerable<IChartStackedBarSeries> stackedBarSeries)
        {
            var percentValue = PercentValue(item);
            var (positiveSum, negativeSum) = PercentSum(barIndex, stackedBarSeries, category(item));

            if (Chart == null)
            {
                return 0;
            }

            if (percentValue >= 0)
            {
                var ticks = Chart.CategoryScale.Ticks(Chart.ValueAxis.TickDistance);
                var sum = Math.Max(ticks.Start, positiveSum);
                return Chart.CategoryScale.Scale(sum);
            }
            else
            {
                var ticks = Chart.CategoryScale.Ticks(Chart.ValueAxis.TickDistance);
                var sum = Math.Max(ticks.Start, negativeSum + percentValue);
                return Chart.CategoryScale.Scale(sum);
            }
        }

        int IChartBarSeries.Count
        {
            get
            {
                return Items.Count;
            }
        }

        double IChartStackedBarSeries.ValueAt(int index)
        {
            if (Items == null || index < 0 || index >= Items.Count)
            {
                return 0;
            }

            return Value(Items[index]);
        }

        IEnumerable<double> IChartStackedBarSeries.ValuesForCategory(double value)
        {
            if (Items == null || Chart == null)
            {
                return Enumerable.Empty<double>();
            }

            var category = ComposeCategory(Chart.ValueScale);

            return Items.Where(item => category(item) == value).Select(Value);
        }

        IEnumerable<object> IChartStackedBarSeries.ItemsForCategory(double value)
        {
            if (Items == null || Chart == null)
            {
                return Enumerable.Empty<object>();
            }

            var category = ComposeCategory(Chart.ValueScale);

            return Items.Where(item => category(item) == value).Cast<object>();
        }

        /// <inheritdoc />
        public override bool Contains(double x, double y, double tolerance)
        {
            return DataAt(x, y).Item1 != null;
        }

        /// <inheritdoc />
        internal override double TooltipX(TItem item)
        {
            if (Chart == null)
            {
                return 0;
            }

            var category = ComposeCategory(Chart.ValueScale);

            return GetBarRight(item, BarIndex, category, StackedBarSeries);
        }

        /// <inheritdoc />
        protected override string TooltipValue(TItem item)
        {
            if (Chart == null)
            {
                return "";
            }

            return Chart.ValueAxis.Format(Chart.CategoryScale, Chart.CategoryScale.Value(Value(item)));
        }

        /// <inheritdoc />
        protected override string TooltipTitle(TItem item)
        {
            if (Chart == null)
            {
                return "";
            }

            var category = Category(Chart.ValueScale);
            return Chart.CategoryAxis.Format(Chart.ValueScale, Chart.ValueScale.Value(category(item)));
        }

        /// <inheritdoc />
        public override (object, Point) DataAt(double x, double y)
        {
            if (Chart == null)
            {
                return (default!, new Point());
            }

            var category = ComposeCategory(Chart.ValueScale);
            var barSeries = VisibleBarSeries;
            var barIndex = BarIndex;

            foreach (var data in Items)
            {
                var startY = GetBarTop(data, category);
                var endY = startY + BandHeight;
                var dataX = GetBarRight(data, barIndex, category, StackedBarSeries);
                if (Chart == null)
                {
                    continue;
                }

                var x0 = GetBarLeft(data, barIndex, category, StackedBarSeries);
                var startX = Math.Min(dataX, x0);
                var endX = Math.Max(dataX, x0);

                if (startX <= x && x <= endX && startY <= y && y <= endY)
                {
                    return (data!, new Point() { X = x, Y = y });
                }
            }

            return (default!, new Point());
        }

        /// <inheritdoc />
        internal override double TooltipY(TItem item)
        {
            return GetBarTop(item) + BarHeight / 2;
        }

        /// <inheritdoc />
        public override IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY)
        {
            if (Chart == null)
            {
                return Enumerable.Empty<ChartDataLabel>();
            }

            var category = ComposeCategory(Chart.ValueScale);
            var list = new List<ChartDataLabel>();
            var barIndex = BarIndex;
            var stackedBarSeries = StackedBarSeries;

            foreach (var data in Items)
            {
                var left = GetBarLeft(data, barIndex, category, stackedBarSeries);
                var right = GetBarRight(data, barIndex, category, stackedBarSeries);
                var x = left + (right - left) / 2;
                list.Add(new ChartDataLabel
                {
                    Position = new Point() { X = x + offsetX, Y = TooltipY(data) + offsetY },
                    TextAnchor = "middle",
                    Text = Chart.ValueAxis.Format(Chart.CategoryScale, Value(data))
                });
            }

            return list;
        }
    }
}
