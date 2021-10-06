using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor
{
    public partial class RadzenColumnSeries<TItem> : CartesianSeries<TItem>, IChartColumnSeries
    {
        [Parameter]
        public string Fill { get; set; }

        [Parameter]
        public IEnumerable<string> Fills { get; set; }

        [Parameter]
        public string Stroke { get; set; }

        [Parameter]
        public IEnumerable<string> Strokes { get; set; }

        [Parameter]
        public double StrokeWidth { get; set; }

        [Parameter]
        public LineType LineType { get; set; }

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

        private double BandWidth
        {
            get
            {
                var availableWidth = Chart.CategoryScale.OutputSize - (Chart.CategoryAxis.Padding * 2);
                var bands = VisibleColumnSeries.Cast<IChartColumnSeries>().Max(series => series.Count) + 2;
                return availableWidth / bands;
            }
        }

        public override bool Contains(double x, double y, double tolerance)
        {
            return DataAt(x, y) != null;
        }

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

        protected override double TooltipY(TItem item)
        {
            var y = base.TooltipY(item);
            var ticks = Chart.ValueScale.Ticks(Chart.ValueAxis.TickDistance);
            var y0 = Chart.ValueScale.Scale(Math.Max(0, ticks.Start));

            return Math.Min(y, y0);
        }
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
