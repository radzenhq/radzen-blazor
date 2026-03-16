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
    /// A chart series that displays data as a funnel chart with trapezoid segments representing stages in a process.
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series.</typeparam>
    public partial class RadzenFunnelSeries<TItem> : CartesianSeries<TItem>
    {
        /// <summary>
        /// Gets or sets a collection of fill colors applied to individual funnel segments.
        /// </summary>
        [Parameter]
        public IEnumerable<string>? Fills { get; set; }

        /// <summary>
        /// Gets or sets a collection of stroke colors applied to individual funnel segments.
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
                foreach (var data in Items)
                {
                    builder.OpenComponent<LegendItem>(0);
                    builder.AddAttribute(1, nameof(LegendItem.Text), TooltipTitle(data));
                    builder.AddAttribute(2, nameof(LegendItem.Class), $"rz-series-item-{Items.IndexOf(data)}");
                    builder.AddAttribute(3, nameof(LegendItem.MarkerSize), MarkerSize);
                    builder.AddAttribute(4, nameof(LegendItem.MarkerType), MarkerType);
                    builder.AddAttribute(5, nameof(LegendItem.Color), PickColor(Items.IndexOf(data), Fills));
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

            return x >= CenterX - AvailableWidth / 2 && x <= CenterX + AvailableWidth / 2
                && y >= TopY && y <= TopY + AvailableHeight;
        }

        /// <inheritdoc />
        public override (object, Point) DataAt(double x, double y)
        {
            if (!Contains(x, y, 0)) return (default!, new Point());

            var count = Items.Count;
            if (count == 0) return (default!, new Point());

            var segHeight = AvailableHeight / count;
            var index = (int)((y - TopY) / segHeight);
            index = Math.Clamp(index, 0, count - 1);

            return (Items[index]!, new Point { X = x, Y = y });
        }

        /// <inheritdoc />
        protected override string TooltipClass(TItem item)
        {
            return $"{base.TooltipClass(item)} rz-funnel-tooltip rz-series-item-{Items.IndexOf(item)}";
        }

        /// <inheritdoc />
        protected override string TooltipStyle(TItem item)
        {
            var style = base.TooltipStyle(item);
            var index = Items.IndexOf(item);
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
            var index = Items.IndexOf(item);
            var count = Items.Count;
            var segHeight = AvailableHeight / count;
            return TopY + index * segHeight + segHeight / 2;
        }

        /// <inheritdoc />
        public override IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY)
        {
            var list = new List<ChartDataLabel>();

            if (Data == null || !Items.Any()) return list;

            var count = Items.Count;
            var maxValue = Items.Max(Value);
            var segHeight = AvailableHeight / count;

            for (int i = 0; i < count; i++)
            {
                var item = Items[i];
                var value = Value(item);
                var topWidth = AvailableWidth * (value / maxValue);
                var y = TopY + i * segHeight + segHeight / 2;

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

        internal string GetSegmentPath(int index, int count, double maxValue)
        {
            var segHeight = AvailableHeight / count;
            var cx = CenterX;

            var value = Value(Items[index]);
            var topWidth = AvailableWidth * (value / maxValue);

            double bottomWidth;
            if (index < count - 1)
            {
                var nextValue = Value(Items[index + 1]);
                bottomWidth = AvailableWidth * (nextValue / maxValue);
            }
            else
            {
                bottomWidth = AvailableWidth * 0.15;
            }

            var topY = this.TopY + index * segHeight;
            var bottomY = topY + segHeight;

            var x1 = cx - topWidth / 2;
            var x2 = cx + topWidth / 2;
            var x3 = cx + bottomWidth / 2;
            var x4 = cx - bottomWidth / 2;

            return $"M {x1.ToInvariantString()} {topY.ToInvariantString()} L {x2.ToInvariantString()} {topY.ToInvariantString()} L {x3.ToInvariantString()} {bottomY.ToInvariantString()} L {x4.ToInvariantString()} {bottomY.ToInvariantString()} Z";
        }
    }
}
