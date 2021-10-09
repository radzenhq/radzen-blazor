using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenColumnSeries.
    /// Implements the <see cref="Radzen.Blazor.CartesianSeries{TItem}" />
    /// Implements the <see cref="Radzen.Blazor.IChartColumnSeries" />
    /// </summary>
    /// <typeparam name="TItem">The type of the t item.</typeparam>
    /// <seealso cref="Radzen.Blazor.CartesianSeries{TItem}" />
    /// <seealso cref="Radzen.Blazor.IChartColumnSeries" />
    public partial class RadzenColumnSeries<TItem> : CartesianSeries<TItem>, IChartColumnSeries
    {
        /// <summary>
        /// Gets or sets the fill.
        /// </summary>
        /// <value>The fill.</value>
        [Parameter]
        public string Fill { get; set; }

        /// <summary>
        /// Gets or sets the fills.
        /// </summary>
        /// <value>The fills.</value>
        [Parameter]
        public IEnumerable<string> Fills { get; set; }

        /// <summary>
        /// Gets or sets the stroke.
        /// </summary>
        /// <value>The stroke.</value>
        [Parameter]
        public string Stroke { get; set; }

        /// <summary>
        /// Gets or sets the strokes.
        /// </summary>
        /// <value>The strokes.</value>
        [Parameter]
        public IEnumerable<string> Strokes { get; set; }

        /// <summary>
        /// Gets or sets the width of the stroke.
        /// </summary>
        /// <value>The width of the stroke.</value>
        [Parameter]
        public double StrokeWidth { get; set; }

        /// <summary>
        /// Gets or sets the type of the line.
        /// </summary>
        /// <value>The type of the line.</value>
        [Parameter]
        public LineType LineType { get; set; }

        /// <summary>
        /// Gets the color.
        /// </summary>
        /// <value>The color.</value>
        public override string Color
        {
            get
            {
                return Fill;
            }
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
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

        /// <summary>
        /// Gets the column series.
        /// </summary>
        /// <value>The column series.</value>
        private IList<IChartSeries> ColumnSeries
        {
            get
            {
                return Chart.Series.Where(series => series is IChartColumnSeries).Cast<IChartSeries>().ToList();
            }
        }

        /// <summary>
        /// Gets the visible column series.
        /// </summary>
        /// <value>The visible column series.</value>
        private IList<IChartSeries> VisibleColumnSeries
        {
            get
            {
                return ColumnSeries.Where(series => series.Visible).ToList();
            }
        }

        /// <summary>
        /// Gets the width of the band.
        /// </summary>
        /// <value>The width of the band.</value>
        private double BandWidth
        {
            get
            {
                var availableWidth = Chart.CategoryScale.OutputSize - (Chart.CategoryAxis.Padding * 2);
                var bands = VisibleColumnSeries.Cast<IChartColumnSeries>().Max(series => series.Count) + 2;
                return availableWidth / bands;
            }
        }

        /// <summary>
        /// Determines whether this instance contains the object.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns><c>true</c> if [contains] [the specified x]; otherwise, <c>false</c>.</returns>
        public override bool Contains(double x, double y, double tolerance)
        {
            return DataAt(x, y) != null;
        }

        /// <summary>
        /// Tooltips the x.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.Double.</returns>
        protected override double TooltipX(TItem item)
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

        /// <summary>
        /// Tooltips the y.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.Double.</returns>
        protected override double TooltipY(TItem item)
        {
            var y = base.TooltipY(item);
            var ticks = Chart.ValueScale.Ticks(Chart.ValueAxis.TickDistance);
            var y0 = Chart.ValueScale.Scale(Math.Max(0, ticks.Start));

            return Math.Min(y, y0);
        }
        /// <summary>
        /// Datas at.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Object.</returns>
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
            var width = bandWidth / columnSeries.Count() - padding + padding / columnSeries.Count();

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
    }
}
