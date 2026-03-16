using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A chart series that displays data as a pyramid chart with trapezoid segments, widest at the bottom.
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series.</typeparam>
    public partial class RadzenPyramidSeries<TItem> : CartesianSeries<TItem>
    {
        /// <summary>
        /// Gets or sets a collection of fill colors applied to individual pyramid segments.
        /// </summary>
        [Parameter]
        public IEnumerable<string>? Fills { get; set; }

        /// <summary>
        /// Gets or sets a collection of stroke colors applied to individual pyramid segments.
        /// </summary>
        [Parameter]
        public IEnumerable<string>? Strokes { get; set; }

        /// <summary>
        /// Gets or sets the width of segment borders in pixels.
        /// </summary>
        [Parameter]
        public double StrokeWidth { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to show labels on each segment.
        /// </summary>
        [Parameter]
        public bool ShowLabels { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to invert the pyramid so the wide base is at the top.
        /// When <c>false</c> (default), the wide base is at the bottom. When <c>true</c>, the wide base is at the top.
        /// </summary>
        [Parameter]
        public bool Inverted { get; set; }

        /// <inheritdoc />
        public override string Color => "#000";

        private double CenterX
        {
            get
            {
                if (Chart?.CategoryScale?.Output != null)
                {
                    return Chart.CategoryScale.Output.Mid + 8;
                }
                return 0;
            }
        }

        private double AvailableWidth
        {
            get
            {
                if (Chart?.CategoryScale?.Output != null)
                {
                    return Chart.CategoryScale.Output.End - Chart.CategoryScale.Output.Start;
                }
                return 0;
            }
        }

        private double AvailableHeight
        {
            get
            {
                if (Chart?.ValueScale?.Output != null)
                {
                    return Chart.ValueScale.Output.End - Chart.ValueScale.Output.Start;
                }
                return 0;
            }
        }

        private double TopY
        {
            get
            {
                if (Chart?.ValueScale?.Output != null)
                {
                    return Chart.ValueScale.Output.Start;
                }
                return 0;
            }
        }

        private IList<TItem> GetSortedItems()
        {
            // Polar coordinate system inverts Y, so code index 0 renders at the visual bottom.
            // Default: largest at index 0 → visual bottom (wide base). Inverted: smallest at index 0 → visual bottom (narrow).
            return Inverted
                ? Items.OrderBy(Value).ToList()
                : Items.OrderByDescending(Value).ToList();
        }

        /// <summary>
        /// Returns the cumulative Y-boundary fractions for each segment. Entry i is the top Y fraction of segment i;
        /// the last entry is 1.0 (bottom of the pyramid). Segment heights are proportional to values.
        /// </summary>
        private double[] GetCumulativeYFractions(IList<TItem> sortedItems)
        {
            var count = sortedItems.Count;
            var totalValue = sortedItems.Sum(Value);
            var fractions = new double[count + 1];
            fractions[0] = 0;
            for (int i = 0; i < count; i++)
            {
                fractions[i + 1] = fractions[i] + (totalValue > 0 ? Value(sortedItems[i]) / totalValue : 1.0 / count);
            }
            return fractions;
        }

        /// <inheritdoc />
        public override double MeasureLegend()
        {
            if (Items.Any())
            {
                return Items.Select(item => TextMeasurer.TextWidth(TooltipTitle(item))).Max() + MarkerSize;
            }

            return 0;
        }

        /// <inheritdoc />
        protected override RenderFragment RenderLegendItem(bool clickable)
        {
            return builder =>
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    var data = Items[i];
                    var index = i;
                    builder.OpenComponent<LegendItem>(0);
                    builder.AddAttribute(1, nameof(LegendItem.Text), TooltipTitle(data));
                    builder.AddAttribute(2, nameof(LegendItem.Class), $"rz-series-item-{index}");
                    builder.AddAttribute(3, nameof(LegendItem.MarkerSize), MarkerSize);
                    builder.AddAttribute(4, nameof(LegendItem.MarkerType), MarkerType);
                    builder.AddAttribute(5, nameof(LegendItem.Color), PickColor(index, Fills));
                    builder.AddAttribute(6, nameof(LegendItem.Click), EventCallback.Factory.Create(this, () => OnLegendClick(data!)));
                    builder.AddAttribute(7, nameof(LegendItem.Clickable), clickable);
                    builder.CloseComponent();
                }
            };
        }

        private async Task OnLegendClick(object data)
        {
            var chart = RequireChart();
            if (chart?.LegendClick.HasDelegate == true)
            {
                await chart.LegendClick.InvokeAsync(new LegendClickEventArgs
                {
                    Data = data,
                    Title = GetTitle(),
                    IsVisible = true,
                });
            }
        }

        /// <inheritdoc />
        public override bool Contains(double x, double y, double tolerance)
        {
            if (!Items.Any()) return false;

            var minY = Math.Min(TopY, TopY + AvailableHeight);
            var maxY = Math.Max(TopY, TopY + AvailableHeight);

            return x >= CenterX - AvailableWidth / 2 && x <= CenterX + AvailableWidth / 2
                && y >= minY && y <= maxY;
        }

        /// <inheritdoc />
        public override (object, Point) DataAt(double x, double y)
        {
            if (!Contains(x, y, 0)) return (default!, new Point());

            var sorted = GetSortedItems();
            var count = sorted.Count;
            if (count == 0) return (default!, new Point());

            var fractions = GetCumulativeYFractions(sorted);
            // AvailableHeight is negative (SVG Y-axis), so yFraction still maps 0→1 correctly
            var yFraction = (y - TopY) / AvailableHeight;
            yFraction = Math.Clamp(yFraction, 0, 1);

            for (int i = 0; i < count; i++)
            {
                if (yFraction <= fractions[i + 1])
                {
                    return (sorted[i]!, new Point { X = x, Y = y });
                }
            }

            return (sorted[count - 1]!, new Point { X = x, Y = y });
        }

        /// <inheritdoc />
        protected override string TooltipClass(TItem item)
        {
            var sorted = GetSortedItems();
            return $"{base.TooltipClass(item)} rz-pyramid-tooltip rz-series-item-{sorted.IndexOf(item)}";
        }

        /// <inheritdoc />
        protected override string TooltipStyle(TItem item)
        {
            var style = base.TooltipStyle(item);
            var sorted = GetSortedItems();
            var index = sorted.IndexOf(item);
            if (index >= 0)
            {
                var color = PickColor(index, Fills);
                if (color != null)
                {
                    style = $"{style}; border-color: {color};";
                }
            }
            return style;
        }

        /// <inheritdoc />
        internal override double TooltipX(TItem item)
        {
            return CenterX;
        }

        /// <inheritdoc />
        internal override double TooltipY(TItem item)
        {
            var sorted = GetSortedItems();
            var index = sorted.IndexOf(item);
            var fractions = GetCumulativeYFractions(sorted);
            var midFraction = (fractions[index] + fractions[index + 1]) / 2;
            return TopY + midFraction * AvailableHeight;
        }

        /// <inheritdoc />
        public override IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY)
        {
            var list = new List<ChartDataLabel>();

            if (Data == null || !Items.Any()) return list;

            var sorted = GetSortedItems();
            var count = sorted.Count;
            var fractions = GetCumulativeYFractions(sorted);

            for (int i = 0; i < count; i++)
            {
                var item = sorted[i];
                var value = Value(item);
                var midFraction = (fractions[i] + fractions[i + 1]) / 2;
                var segmentHeight = Math.Abs((fractions[i + 1] - fractions[i]) * AvailableHeight);

                // Skip labels for segments that are too thin to display text
                if (segmentHeight < 16)
                {
                    continue;
                }

                var y = TopY + midFraction * AvailableHeight;

                var chart = RequireChart();
                if (chart != null)
                {
                    list.Add(new ChartDataLabel
                    {
                        TextAnchor = "middle",
                        Position = new Point { X = CenterX + offsetX, Y = y + offsetY },
                        Text = chart.ValueAxis.Format(chart.ValueScale, value)
                    });
                }
            }

            return list;
        }

        internal string GetSegmentPath(int index, IList<TItem> sortedItems)
        {
            var cx = CenterX;
            var baseWidth = AvailableWidth;
            var fractions = GetCumulativeYFractions(sortedItems);

            // Y positions — segment height is proportional to its value
            var topYFraction = fractions[index];
            var bottomYFraction = fractions[index + 1];
            var topY = this.TopY + topYFraction * AvailableHeight;
            var bottomY = this.TopY + bottomYFraction * AvailableHeight;

            // Width at each Y follows the triangle's straight sides
            double topWidthFraction;
            double bottomWidthFraction;

            if (Inverted)
            {
                // Polar Y-inversion: wide base at visual top, apex at visual bottom
                topWidthFraction = topYFraction;
                bottomWidthFraction = bottomYFraction;
            }
            else
            {
                // Polar Y-inversion: apex at visual top, wide base at visual bottom
                topWidthFraction = 1.0 - topYFraction;
                bottomWidthFraction = 1.0 - bottomYFraction;
            }

            var topWidth = baseWidth * topWidthFraction;
            var bottomWidth = baseWidth * bottomWidthFraction;

            var x1 = cx - topWidth / 2;
            var x2 = cx + topWidth / 2;
            var x3 = cx + bottomWidth / 2;
            var x4 = cx - bottomWidth / 2;

            return $"M {x1.ToInvariantString()} {topY.ToInvariantString()} L {x2.ToInvariantString()} {topY.ToInvariantString()} L {x3.ToInvariantString()} {bottomY.ToInvariantString()} L {x4.ToInvariantString()} {bottomY.ToInvariantString()} Z";
        }
    }
}
