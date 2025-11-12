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
    public partial class RadzenBarSeries<TItem> : CartesianSeries<TItem>, IChartBarSeries
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
            return base.TransformValueScale(scale);
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

        private IList<IChartSeries> BarSeries
        {
            get
            {
                var chart = Chart;
                if (chart == null)
                {
                    return new List<IChartSeries>();
                }

                return chart.Series.Where(series => series is IChartBarSeries).Cast<IChartSeries>().ToList();
            }
        }

        private IList<IChartSeries> VisibleBarSeries
        {
            get
            {
                return BarSeries.Where(series => series.Visible).ToList();
            }
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
                if (barSeries.Count == 0)
                {
                    return 0;
                }

                var chart = Chart;
                if (chart == null)
                {
                    return 0;
                }

                var barOptions = chart.BarOptions;

                if (barOptions?.Height.HasValue == true)
                {
                    return barOptions.Height.Value * barSeries.Count;
                }
                else
                {
                    var availableHeight = chart.ValueScale.OutputSize; // - (Chart.ValueAxis.Padding * 2);
                    var bands = barSeries.Cast<IChartBarSeries>().Max(series => series.Count) + 2;
                    return availableHeight / bands;
                }
            }
        }

        int IChartBarSeries.Count
        {
            get
            {
                return Items.Count;
            }
        }

        /// <inheritdoc />
        public override bool Contains(double x, double y, double tolerance)
        {
            return DataAt(x, y).Item1 != null;
        }

        /// <inheritdoc />
        internal override double TooltipX(TItem item)
        {
            var chart = Chart;
            if (chart == null)
            {
                return 0;
            }

            var value = chart.CategoryScale.Compose(Value);
            var x = value(item);

            return x;
        }

        /// <inheritdoc />
        protected override string TooltipValue(TItem item)
        {
            var chart = Chart;
            if (chart == null)
            {
                return string.Empty;
            }

            return chart.ValueAxis.Format(chart.CategoryScale, chart.CategoryScale.Value(Value(item)));
        }

        /// <inheritdoc />
        protected override string TooltipTitle(TItem item)
        {
            var chart = Chart;
            if (chart == null)
            {
                return string.Empty;
            }

            var category = Category(chart.ValueScale);
            return chart.CategoryAxis.Format(chart.ValueScale, chart.ValueScale.Value(category(item)));
        }

        /// <inheritdoc />
        public override (object, Point) DataAt(double x, double y)
        {
            var chart = Chart;
            if (chart == null)
            {
                return (default!, new Point());
            }

            var value = ComposeValue(chart.CategoryScale);
            var category = ComposeCategory(chart.ValueScale);
            var ticks = chart.CategoryScale.Ticks(chart.ValueAxis.TickDistance);
            var x0 = chart.CategoryScale.Scale(Math.Max(0, ticks.Start));

            var barSeries = VisibleBarSeries;
            var index = barSeries.IndexOf(this);
            if (barSeries.Count == 0 || index < 0)
            {
                return (default!, new Point());
            }

            var padding = chart.BarOptions?.Margin ?? 0;
            var bandHeight = BandHeight;
            var height = barSeries.Count > 0 ? bandHeight / barSeries.Count - padding + padding / barSeries.Count : 0;

            foreach (var data in Items)
            {
                var startY = category(data) - bandHeight / 2 + index * height + index * padding;
                var endY = startY + height;
                var dataX = value(data);
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
            var chart = Chart;
            if (chart == null)
            {
                return 0;
            }

            var category = ComposeCategory(chart.ValueScale);
            var barSeries = VisibleBarSeries;
            var index = barSeries.IndexOf(this);
            if (barSeries.Count == 0 || index < 0)
            {
                return 0;
            }

            var padding = chart.BarOptions?.Margin ?? 0;
            var bandHeight = BandHeight;
            var height = barSeries.Count > 0 ? bandHeight / barSeries.Count - padding + padding / barSeries.Count : 0;
            var y = category(item) - bandHeight / 2 + index * height + index * padding;

            return y + height / 2;
        }

        /// <inheritdoc />
        public override IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY)
        {
            var list = new List<ChartDataLabel>();
            var chart = Chart;
            if (chart == null)
            {
                return list;
            }

            (string Anchor, int Sign) position;

            if (Data != null)
            {
                foreach (var d in Data)
                {
                    position = Value(d) < 0 ? ("end", -1) : Value(d) == 0 ? ("middle", 0) : ("start", 1);

                    list.Add(new ChartDataLabel
                    {
                        Position = new Point() { X = TooltipX(d) + offsetX + (8 * position.Sign), Y = TooltipY(d) + offsetY },
                        TextAnchor = position.Anchor,
                        Text = chart.ValueAxis.Format(chart.CategoryScale, Value(d))
                    });
                }
            }

            return list;
        }
    }
}