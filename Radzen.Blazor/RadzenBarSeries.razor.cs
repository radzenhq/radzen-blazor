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
                return Chart.Series.Where(series => series is IChartBarSeries).Cast<IChartSeries>().ToList();
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
            var value = Chart.CategoryScale.Compose(Value);
            var x = value(item);

            return x;
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
        public override (object, Point) DataAt(double x, double y)
        {
            var value = ComposeValue(Chart.CategoryScale);
            var category = ComposeCategory(Chart.ValueScale);
            var ticks = Chart.CategoryScale.Ticks(Chart.ValueAxis.TickDistance);
            var x0 = Chart.CategoryScale.Scale(Math.Max(0, ticks.Start));

            var barSeries = VisibleBarSeries;
            var index = barSeries.IndexOf(this);
            var padding = Chart.BarOptions.Margin;
            var bandHeight = BandHeight;
            var height = bandHeight / barSeries.Count() - padding + padding / barSeries.Count();

            foreach (var data in Items)
            {
                var startY = category(data) - bandHeight / 2 + index * height + index * padding;
                var endY = startY + height;
                var dataX = value(data);
                var startX = Math.Min(dataX, x0);
                var endX = Math.Max(dataX, x0);

                if (startX <= x && x <= endX && startY <= y && y <= endY)
                {
                    return (data, new Point() { X = x, Y = y });
                }
            }

            return (null, null);
        }

        /// <inheritdoc />
        internal override double TooltipY(TItem item)
        {
            var category = ComposeCategory(Chart.ValueScale);
            var barSeries = VisibleBarSeries;
            var index = barSeries.IndexOf(this);
            var padding = Chart.BarOptions.Margin;
            var bandHeight = BandHeight;
            var height = bandHeight / barSeries.Count() - padding + padding / barSeries.Count();
            var y = category(item) - bandHeight / 2 + index * height + index * padding;

            return y + height / 2;
        }

        /// <inheritdoc />
        public override IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY)
        {
            var list = new List<ChartDataLabel>();

            foreach (var d in Data)
            {
                list.Add(new ChartDataLabel 
                { 
                    Position = new Point() { X = TooltipX(d) + offsetX + 8, Y = TooltipY(d) + offsetY },
                    TextAnchor = "start",
                    Text = Chart.ValueAxis.Format(Chart.CategoryScale, Value(d))
                });
            }

            return list;
        }
    }
}
