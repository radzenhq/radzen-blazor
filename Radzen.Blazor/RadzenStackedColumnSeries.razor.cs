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

        double IChartStackedColumnSeries.ValueAt(int index)
        {
            if (Items == null || index < 0 || index >= Items.Count)
            {
                return 0;
            }

            return Value(Items[index]);
        }

        double GetOffset(ScaleBase scale, int i)
        {
            var columnSeries = VisibleColumnSeries;
            var index = columnSeries.IndexOf(this);

            return scale.Scale(columnSeries.Cast<IChartStackedColumnSeries>().Take(index).Sum(s => s.ValueAt(i)));
        }

        double IChartStackedColumnSeries.Max
        {
            get
            {
                if (Items == null)
                {
                    return 0;
                }

                return Items.Max(Value);
            }
        }

        private IList<IChartSeries> ColumnSeries
        {
            get
            {
                return Chart.Series.Where(series => series is IChartStackedColumnSeries).Cast<IChartSeries>().ToList();
            }
        }

        private IList<IChartSeries> VisibleColumnSeries
        {
            get
            {
                return ColumnSeries.Where(series => series.Visible).ToList();
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
            return DataAt(x, y) != null;
        }

        /// <inheritdoc />
        internal override double TooltipX(TItem item)
        {
            var columnSeries = VisibleColumnSeries;
            var index = columnSeries.IndexOf(this);
            var padding = Chart.ColumnOptions.Margin;
            var bandWidth = BandWidth;
            var width = bandWidth / columnSeries.Count() - padding + padding / columnSeries.Count();
            var category = ComposeCategory(Chart.CategoryScale);
            var x = category(item) - bandWidth / 2 + index * width + index * padding;

            return x + width / 2;
        }

        /// <inheritdoc />
        internal override double TooltipY(TItem item)
        {
            var y = base.TooltipY(item);
            var ticks = Chart.ValueScale.Ticks(Chart.ValueAxis.TickDistance);
            var y0 = Chart.ValueScale.Scale(Math.Max(0, ticks.Start));

            return Math.Min(y, y0);
        }

        /// <inheritdoc />
        public override object DataAt(double x, double y)
        {
            var category = ComposeCategory(Chart.CategoryScale);
            var value = ComposeValue(Chart.ValueScale);
            var ticks = Chart.ValueScale.Ticks(Chart.ValueAxis.TickDistance);
            var y0 = Chart.ValueScale.Scale(Math.Max(0, ticks.Start));

            var columnSeries = VisibleColumnSeries;
            var index = columnSeries.IndexOf(this);
            var padding = Chart.ColumnOptions.Margin;
            var bandWidth = BandWidth;
            var width = Chart.ColumnOptions.Width ?? bandWidth / columnSeries.Count() - padding + padding / columnSeries.Count();

            foreach (var data in Items)
            {
                var startX = category(data) - bandWidth / 2 + index * width + index * padding;
                var endX = startX + width;
                var dataY = value(data);
                var startY = Math.Min(dataY, y0);
                var endY = Math.Max(dataY, y0);

                if (startX <= x && x <= endX && startY <= y && y <= endY)
                {
                    return data;
                }
            }

            return null;
        }

        /// <inheritdoc />
        public override IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY)
        {
            return base.GetDataLabels(offsetX, offsetY - 16);
        }

        /// <inheritdoc />
        public override ScaleBase TransformValueScale(ScaleBase scale)
        {
            var stackedColumnSeries = ColumnSeries.Cast<IChartStackedColumnSeries>();
            var max = stackedColumnSeries.Aggregate(0d, (acc, series) => acc + series.Max);
            var min = Items.Min(Value);

            scale.Input.MergeWidth(new ScaleRange { Start = min, End = max });

            return scale;
        }
    }
}
