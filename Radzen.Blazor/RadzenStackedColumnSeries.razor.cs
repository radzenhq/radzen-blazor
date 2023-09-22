using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// Renders column series in <see cref="RadzenChart" />
    /// </summary>
    /// <typeparam name="TItem">The type of the series data item.</typeparam>
    public partial class RadzenStackedColumnSeries<TItem> : CartesianSeries<TItem>, IChartStackedColumnSeries
    {
        /// <summary>
        /// Specifies the fill (background color) of the column series.
        /// </summary>
        /// <value>The fill.</value>
        [Parameter]
        public string Fill { get; set; }

        /// <summary>
        /// Specifies a list of colors that will be used to set the individual column backgrounds.
        /// </summary>
        /// <value>The fills.</value>
        [Parameter]
        public IEnumerable<string> Fills { get; set; }

        /// <summary>
        /// Specifies the stroke (border color) of the column series.
        /// </summary>
        /// <value>The stroke.</value>
        [Parameter]
        public string Stroke { get; set; }

        /// <summary>
        /// Specifies a list of colors that will be used to set the individual column borders.
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
        /// Gets or sets the type of the line used to render the column border.
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

        int IChartStackedColumnSeries.Count
        {
            get
            {
                if (Items == null)
                {
                    return 0;
                }

                return Items.Count();
            }
        }

        IEnumerable<double> IChartStackedColumnSeries.ValuesForCategory(double value)
        {
            if (Items == null)
            {
                return Enumerable.Empty<double>();
            }

            var category = ComposeCategory(Chart.CategoryScale);

            return Items.Where(item => category(item) == value).Select(Value);
        }

        double IChartStackedColumnSeries.ValueAt(int index)
        {
            if (Items == null || index < 0 || index >= Items.Count)
            {
                return 0;
            }

            return Value(Items[index]);
        }

        private IList<IChartSeries> ColumnSeries => Chart.Series.Where(series => series is IChartStackedColumnSeries).Cast<IChartSeries>().ToList();

        private IList<IChartSeries> VisibleColumnSeries => ColumnSeries.Where(series => series.Visible).ToList();

        private IList<IChartStackedColumnSeries> StackedColumnSeries => VisibleColumnSeries.Cast<IChartStackedColumnSeries>().ToList();

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

        private double BandWidth
        {
            get
            {
                var columnSeries = VisibleColumnSeries;

                if (Chart.ColumnOptions.Width.HasValue)
                {
                    return Chart.ColumnOptions.Width.Value * columnSeries.Count + Chart.ColumnOptions.Margin * (columnSeries.Count - 1);
                }
                else
                {
                    var availableWidth = Chart.CategoryScale.OutputSize - (Chart.CategoryAxis.Padding * 2);
                    var bands = columnSeries.Cast<IChartStackedColumnSeries>().Max(series => series.Count) + 2;
                    return availableWidth / bands;
                }
            }
        }

        /// <inheritdoc />
        public override bool Contains(double x, double y, double tolerance)
        {
            return DataAt(x, y).Item1 != null;
        }

        double ColumnWidth => Chart.ColumnOptions.Width ?? BandWidth - Chart.ColumnOptions.Margin;

        private double GetColumnLeft(TItem item, Func<TItem, double> category = null)
        {
            category = category ?? ComposeCategory(Chart.CategoryScale);

            return category(item) - ColumnWidth / 2;
        }

        private double GetColumnTop(TItem item, int columnIndex, Func<TItem, double> category, IEnumerable<IChartStackedColumnSeries> stackedColumnSeries)
        {
            var sum = Sum(columnIndex, stackedColumnSeries, category(item));

            var y = Chart.ValueScale.Scale(Value(item) + sum);

            return y;
        }

        private static double Sum(int columnIndex, IEnumerable<IChartStackedColumnSeries> stackedColumnSeries, double category)
        {
            return stackedColumnSeries.Take(columnIndex).SelectMany(series => series.ValuesForCategory(category)).DefaultIfEmpty(0).Sum();
        }

        private double GetColumnBottom(TItem item, int columnIndex, Func<TItem, double> category, IEnumerable<IChartStackedColumnSeries> stackedColumnSeries)
        {
            var ticks = Chart.ValueScale.Ticks(Chart.ValueAxis.TickDistance);

            var sum = Sum(columnIndex, stackedColumnSeries, category(item));

            return Chart.ValueScale.Scale(Math.Max(0, Math.Max(ticks.Start, sum)));
        }

        int ColumnIndex => VisibleColumnSeries.IndexOf(this);

        /// <inheritdoc />
        internal override double TooltipX(TItem item)
        {
            return GetColumnLeft(item) + ColumnWidth / 2;
        }

        /// <inheritdoc />
        internal override double TooltipY(TItem item)
        {
            var category = ComposeCategory(Chart.CategoryScale);

            return GetColumnTop(item, ColumnIndex, category, StackedColumnSeries);
        }

        /// <inheritdoc />
        public override (object, Point) DataAt(double x, double y)
        {
            var category = ComposeCategory(Chart.CategoryScale);
            var columnIndex = ColumnIndex;
            var width = ColumnWidth;
            var stackedColumnSeries = StackedColumnSeries;

            foreach (var data in Items)
            {
                var startX = GetColumnLeft(data, category);
                var endX = startX + width;
                var dataY = GetColumnTop(data, columnIndex, category, stackedColumnSeries);
                var y0 = GetColumnBottom(data, columnIndex, category, stackedColumnSeries);
                var startY = Math.Min(dataY, y0);
                var endY = Math.Max(dataY, y0);

                if (startX <= x && x <= endX && startY <= y && y <= endY)
                {
                    return (data, new Point() { X = x, Y = y });
                }
            }

            return (null, null);
        }

        /// <inheritdoc />
        public override IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY)
        {
            var list = new List<ChartDataLabel>();
            var stackedColumnSeries = StackedColumnSeries;
            var columnIndex = ColumnIndex;
            var category = ComposeCategory(Chart.CategoryScale);

            foreach (var data in Items)
            {
                var top = GetColumnTop(data, columnIndex, category, stackedColumnSeries);
                var bottom = GetColumnBottom(data, columnIndex, category, stackedColumnSeries);
                var y = top + (bottom - top) / 2;

                list.Add(new ChartDataLabel
                {
                    Position = new Point { X = TooltipX(data) + offsetX, Y = y + offsetY },
                    TextAnchor = "middle",
                    Text = Chart.ValueAxis.Format(Chart.ValueScale, Value(data))
                });
            }

            return list;
        }

        /// <inheritdoc />
        public override ScaleBase TransformValueScale(ScaleBase scale)
        {
            if (Items.Any())
            {
                var stackedColumnSeries = ColumnSeries.Cast<IChartStackedColumnSeries>();
                var count = stackedColumnSeries.Max(series => series.Count);
                var sums = Enumerable.Range(0, count).Select(i => stackedColumnSeries.Sum(series => series.ValueAt(i)));
                var max = sums.Max();
                var min = Items.Min(Value);

                scale.Input.MergeWidth(new ScaleRange { Start = min, End = max });
            }

            return scale;
        }
    }
}
