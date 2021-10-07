using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenBarSeries.
    /// Implements the <see cref="Radzen.Blazor.CartesianSeries{TItem}" />
    /// Implements the <see cref="Radzen.Blazor.IChartBarSeries" />
    /// </summary>
    /// <typeparam name="TItem">The type of the t item.</typeparam>
    /// <seealso cref="Radzen.Blazor.CartesianSeries{TItem}" />
    /// <seealso cref="Radzen.Blazor.IChartBarSeries" />
    public partial class RadzenBarSeries<TItem> : Radzen.Blazor.CartesianSeries<TItem>, IChartBarSeries
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
        /// Transforms the category scale.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <returns>ScaleBase.</returns>
        public override ScaleBase TransformCategoryScale(ScaleBase scale)
        {
            return base.TransformValueScale(scale);
        }

        /// <summary>
        /// Transforms the value scale.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <returns>ScaleBase.</returns>
        public override ScaleBase TransformValueScale(ScaleBase scale)
        {
            return base.TransformCategoryScale(scale);
        }

        /// <summary>
        /// Gets the bar series.
        /// </summary>
        /// <value>The bar series.</value>
        private IList<IChartSeries> BarSeries
        {
            get
            {
                return Chart.Series.Where(series => series is IChartBarSeries).Cast<IChartSeries>().ToList();
            }
        }

        /// <summary>
        /// Gets the visible bar series.
        /// </summary>
        /// <value>The visible bar series.</value>
        private IList<IChartSeries> VisibleBarSeries
        {
            get
            {
                return BarSeries.Where(series => series.Visible).ToList();
            }
        }

        /// <summary>
        /// Gets the height of the band.
        /// </summary>
        /// <value>The height of the band.</value>
        private double BandHeight
        {
            get
            {
                var availableHeight = Chart.ValueScale.OutputSize; // - (Chart.ValueAxis.Padding * 2);
                var bands = VisibleBarSeries.Cast<IChartBarSeries>().Max(series => series.Count) + 2;
                return availableHeight / bands;
            }
        }

        /// <summary>
        /// Gets the count.
        /// </summary>
        /// <value>The count.</value>
        int IChartBarSeries.Count
        {
            get
            {
                return Items.Count;
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
            var value = Chart.CategoryScale.Compose(Value);
            var x = value(item);

            return x;
        }

        /// <summary>
        /// Tooltips the value.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected override string TooltipValue(TItem item)
        {
            return Chart.ValueAxis.Format(Chart.CategoryScale, Chart.CategoryScale.Value(Value(item)));
        }
        /// <summary>
        /// Tooltips the title.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected override string TooltipTitle(TItem item)
        {
            var category = Category(Chart.ValueScale);
            return Chart.CategoryAxis.Format(Chart.ValueScale, Chart.ValueScale.Value(category(item)));
        }

        /// <summary>
        /// Datas at.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Object.</returns>
        public override object DataAt(double x, double y)
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
                    return data;
                }
            }

            return null;
        }

        /// <summary>
        /// Tooltips the y.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.Double.</returns>
        protected override double TooltipY(TItem item)
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
    }
}