using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// Renders bar series in <see cref="RadzenChart" />.
    /// </summary>
    /// <typeparam name="TItem">The type of the series data item.</typeparam>
    public partial class RadzenStackedBarSeries<TItem> : CartesianSeries<TItem>, IChartStackedBarSeries
    {
        /// <summary>
        /// Specifies the fill (background color) of the bar series.
        /// </summary>
        /// <value>The fill.</value>
        [Parameter]
        public string Fill { get; set; }

        /// <summary>
        /// Specifies a list of colors that will be used to set the individual bar backgrounds.
        /// </summary>
        /// <value>The fills.</value>
        [Parameter]
        public IEnumerable<string> Fills { get; set; }

        /// <summary>
        /// Specifies the stroke (border color) of the bar series.
        /// </summary>
        /// <value>The stroke.</value>
        [Parameter]
        public string Stroke { get; set; }

        /// <summary>
        /// Specifies a list of colors that will be used to set the individual bar borders.
        /// </summary>
        /// <value>The strokes.</value>
        [Parameter]
        public IEnumerable<string> Strokes { get; set; }

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

        /// <inheritdoc />
        public override string Color
        {
            get
            {
                return Fill;
            }
        }

        /// <inheritdoc />
        public override ScaleBase TransformCategoryScale(ScaleBase scale)
        {
            var stackedBarSeries = BarSeries.Cast<IChartStackedBarSeries>();
            var count = stackedBarSeries.Max(series => series.Count);
            var sums = Enumerable.Range(0, count).Select(i => stackedBarSeries.Sum(series => series.ValueAt(i)));
            var max = sums.Max();
            var min = Items.Min(Value);

            scale.Input.MergeWidth(new ScaleRange { Start = min, End = max });

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

        private IList<IChartSeries> BarSeries => Chart.Series.Where(series => series is IChartStackedBarSeries).Cast<IChartSeries>().ToList();

        private IList<IChartSeries> VisibleBarSeries => BarSeries.Where(series => series.Visible).ToList();

        private IList<IChartStackedBarSeries> StackedBarSeries => VisibleBarSeries.Cast<IChartStackedBarSeries>().ToList();

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

        private double BandHeight
        {
            get
            {
                var barSeries = VisibleBarSeries;

                if (Chart.BarOptions.Height.HasValue)
                {
                    return Chart.BarOptions.Height.Value * barSeries.Count;
                }
                else
                {
                    var availableHeight = Chart.ValueScale.OutputSize; // - (Chart.ValueAxis.Padding * 2);
                    var bands = barSeries.Cast<IChartStackedBarSeries>().Max(series => series.Count) + 2;
                    return availableHeight / bands;
                }
            }
        }

        double BarHeight => Chart.BarOptions.Height ?? BandHeight - Chart.BarOptions.Margin;

        int BarIndex => VisibleBarSeries.IndexOf(this);

        private double GetBarTop(TItem item, Func<TItem, double> category = null)
        {
            category = category ?? ComposeCategory(Chart.ValueScale);

            return category(item) - BarHeight / 2;
        }

        private double GetBarRight(TItem item, int barIndex, int index, IEnumerable<IChartStackedBarSeries> stackedBarSeries)
        {
            var count = stackedBarSeries.Max(series => series.Count);
            var sum = stackedBarSeries.Take(barIndex).Sum(series => series.ValueAt(index));

            var y = Chart.CategoryScale.Scale(Value(item) + sum);

            return y;
        }

        private double GetBarLeft(int barIndex, int index, IEnumerable<IChartStackedBarSeries> stackedBarSeries)
        {
            var ticks = Chart.CategoryScale.Ticks(Chart.ValueAxis.TickDistance);

            var sum = stackedBarSeries.Take(barIndex).Sum(series => series.ValueAt(index));

            return Chart.CategoryScale.Scale(Math.Max(0, Math.Max(ticks.Start, sum)));
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

        /// <inheritdoc />
        public override bool Contains(double x, double y, double tolerance)
        {
            return DataAt(x, y) != null;
        }

        /// <inheritdoc />
        internal override double TooltipX(TItem item)
        {
            return GetBarRight(item, BarIndex, Items.IndexOf(item), StackedBarSeries);
        }

        /// <inheritdoc />
        protected override string TooltipValue(TItem item)
        {
            return Chart.ValueAxis.Format(Chart.CategoryScale, Chart.CategoryScale.Value(Value(item)));
        }

        /// <inheritdoc />
        protected override string TooltipTitle(TItem item)
        {
            var category = Category(Chart.ValueScale);
            return Chart.CategoryAxis.Format(Chart.ValueScale, Chart.ValueScale.Value(category(item)));
        }

        /// <inheritdoc />
        public override object DataAt(double x, double y)
        {
            var category = ComposeCategory(Chart.ValueScale);
            var barSeries = VisibleBarSeries;
            var barIndex = BarIndex;

            for (var index = 0; index < Items.Count; index++)
            {
                var data = Items[index];
                var startY = GetBarTop(data, category);
                var endY = startY + BandHeight;
                var dataX = GetBarRight(data, barIndex, index, StackedBarSeries);
                var x0 = GetBarLeft(barIndex, index, StackedBarSeries);
                var startX = Math.Min(dataX, x0);
                var endX = Math.Max(dataX, x0);

                if (startX <= x && x <= endX && startY <= y && y <= endY)
                {
                    return data;
                }
            }

            return null;
        }

        /// <inheritdoc />
        internal override double TooltipY(TItem item)
        {
            return GetBarTop(item) + BarHeight / 2;
        }

        /// <inheritdoc />
        public override IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY)
        {
            var list = new List<ChartDataLabel>();
            var barIndex = BarIndex;
            var stackedBarSeries = StackedBarSeries;

            for (var index = 0; index < Items.Count; index++)
            {
                var data = Items[index];
                var left = GetBarLeft(barIndex, index, stackedBarSeries);
                var right = GetBarRight(data, barIndex, index, stackedBarSeries);
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
