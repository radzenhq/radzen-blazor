using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// A chart series that displays data as vertical columns (bars) in a RadzenChart.
    /// RadzenColumnSeries is ideal for comparing values across categories or showing trends over time with discrete data points.
    /// Renders vertical rectangles where the height represents the data value. Multiple column series in the same chart are displayed side-by-side for each category.
    /// Supports fill color/stroke color/width customization with individual column colors via Fills/Strokes, dynamic coloring based on value ranges using FillRange and StrokeRange,
    /// optional value labels on top of columns, interactive tooltips showing category/value/series name, and click event handling for drill-down scenarios.
    /// Use CategoryProperty to specify the X-axis field and ValueProperty for the column height (Y-axis value).
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series. Each item represents one column in the chart.</typeparam>
    /// <example>
    /// Basic column series:
    /// <code>
    /// &lt;RadzenChart&gt;
    ///     &lt;RadzenColumnSeries Data=@revenue CategoryProperty="Quarter" ValueProperty="Amount" Title="Revenue" /&gt;
    /// &lt;/RadzenChart&gt;
    /// </code>
    /// Multiple column series with custom colors:
    /// <code>
    /// &lt;RadzenChart&gt;
    ///     &lt;RadzenColumnSeries Data=@sales2023 CategoryProperty="Month" ValueProperty="Total" Title="2023" Fill="#4169E1" /&gt;
    ///     &lt;RadzenColumnSeries Data=@sales2024 CategoryProperty="Month" ValueProperty="Total" Title="2024" Fill="#32CD32" /&gt;
    /// &lt;/RadzenChart&gt;
    /// </code>
    /// </example>
    public partial class RadzenColumnSeries<TItem> : CartesianSeries<TItem>, IChartColumnSeries
    {
        /// <summary>
        /// Gets or sets the fill (background) color applied to all columns in the series.
        /// Supports any valid CSS color value. If not set, uses the color scheme's default color.
        /// </summary>
        /// <value>The fill color as a CSS color value.</value>
        [Parameter]
        public string Fill { get; set; }

        /// <summary>
        /// Gets or sets a collection of fill colors to apply to individual columns in sequence.
        /// Each column gets the color at its index position, allowing rainbow or gradient-like effects.
        /// Takes precedence over the <see cref="Fill"/> property.
        /// </summary>
        /// <value>An enumerable collection of CSS color values.</value>
        [Parameter]
        public IEnumerable<string> Fills { get; set; }

        /// <summary>
        /// Gets or sets the stroke (border) color applied to all columns in the series.
        /// If not set, columns render without borders.
        /// </summary>
        /// <value>The stroke color as a CSS color value.</value>
        [Parameter]
        public string Stroke { get; set; }

        /// <summary>
        /// Gets or sets a collection of stroke colors to apply to individual column borders in sequence.
        /// Each column border gets the color at its index position.
        /// Takes precedence over the <see cref="Stroke"/> property.
        /// </summary>
        /// <value>An enumerable collection of CSS color values for borders.</value>
        [Parameter]
        public IEnumerable<string> Strokes { get; set; }

        /// <summary>
        /// Gets or sets the width of the column border in pixels.
        /// Only visible if <see cref="Stroke"/> or <see cref="Strokes"/> is specified.
        /// </summary>
        /// <value>The stroke width in pixels. Default is 0 (no border).</value>
        [Parameter]
        public double StrokeWidth { get; set; }

        /// <summary>
        /// Gets or sets the line style for column borders (solid, dashed, dotted).
        /// Only applicable if stroke is enabled.
        /// </summary>
        /// <value>The line type. Default is solid.</value>
        [Parameter]
        public LineType LineType { get; set; }

        /// <summary>
        /// Gets or sets value-based color ranges that dynamically color columns based on their values.
        /// Allows conditional coloring (e.g., red for negative values, green for positive).
        /// Each range specifies a min/max value and a color to apply to columns within that range.
        /// </summary>
        /// <value>A collection of value ranges and their associated fill colors.</value>
        [Parameter]
        public IList<SeriesColorRange> FillRange { get; set; }

        /// <summary>
        /// Gets or sets value-based color ranges that dynamically color column borders based on their values.
        /// Works similarly to <see cref="FillRange"/> but affects the stroke color instead of fill.
        /// </summary>
        /// <value>A collection of value ranges and their associated stroke colors.</value>
        [Parameter]
        public IList<SeriesColorRange> StrokeRange { get; set; }

        /// <inheritdoc />
        public override string Color
        {
            get
            {
                return Fill;
            }
        }

        int IChartColumnSeries.Count
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

        private IList<IChartSeries> ColumnSeries
        {
            get
            {
                return Chart.Series.Where(series => series is IChartColumnSeries).Cast<IChartSeries>().ToList();
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
                var color = PickColor(index, Fills, Fill, FillRange, Value(item));

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
                    var bands = columnSeries.Cast<IChartColumnSeries>().Max(series => series.Count) + 2;
                    return availableWidth / bands;
                }
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
            return base.TooltipY(item);
        }

        /// <inheritdoc />
        public override (object, Point) DataAt(double x, double y)
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
                    return (data, new Point() { X = x, Y = y });
                }
            }

            return (null, null);
        }

        /// <inheritdoc />
        public override IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY)
        {
            var list = new List<ChartDataLabel>();

            int sign;

            foreach (var d in Data)
            {
                sign = Value(d) < 0 ? -1 : Value(d) == 0 ? 0 : 1;

                list.Add(new ChartDataLabel
                {
                    Position = new Point() { X = TooltipX(d) + offsetX, Y = TooltipY(d) - offsetY - (16 * sign) },
                    TextAnchor = "middle",
                    Text = Chart.ValueAxis.Format(Chart.ValueScale, Value(d))
                });
            }

            return list;
        }
    }
}