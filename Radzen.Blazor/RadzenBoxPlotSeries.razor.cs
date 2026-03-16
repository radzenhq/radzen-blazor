using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A chart series that displays statistical data as box-and-whisker plots in a RadzenChart.
    /// Each box plot represents five values: LowerWhisker (min), LowerQuartile (Q1), Median (Q2), UpperQuartile (Q3), and UpperWhisker (max).
    /// Optionally displays a mean marker.
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series.</typeparam>
    public partial class RadzenBoxPlotSeries<TItem> : CartesianSeries<TItem>
    {
        /// <summary>
        /// Gets or sets the name of the property that provides the lower whisker (minimum) value.
        /// </summary>
        [Parameter]
        public string? LowerWhiskerProperty { get; set; }

        /// <summary>
        /// Gets or sets the name of the property that provides the lower quartile (Q1, 25th percentile) value.
        /// </summary>
        [Parameter]
        public string? LowerQuartileProperty { get; set; }

        /// <summary>
        /// Gets or sets the name of the property that provides the median (Q2, 50th percentile) value.
        /// </summary>
        [Parameter]
        public string? MedianProperty { get; set; }

        /// <summary>
        /// Gets or sets the name of the property that provides the upper quartile (Q3, 75th percentile) value.
        /// </summary>
        [Parameter]
        public string? UpperQuartileProperty { get; set; }

        /// <summary>
        /// Gets or sets the name of the property that provides the upper whisker (maximum) value.
        /// </summary>
        [Parameter]
        public string? UpperWhiskerProperty { get; set; }

        /// <summary>
        /// Gets or sets the name of the property that provides the mean value. Optional.
        /// </summary>
        [Parameter]
        public string? MeanProperty { get; set; }

        /// <summary>
        /// Gets or sets the fill color for the box.
        /// </summary>
        [Parameter]
        public string? Fill { get; set; }

        /// <summary>
        /// Gets or sets the stroke color for the box, whiskers, and median line.
        /// </summary>
        [Parameter]
        public string? Stroke { get; set; }

        /// <summary>
        /// Gets or sets the stroke color for the median line. Falls back to <see cref="Stroke"/>.
        /// </summary>
        [Parameter]
        public string? MedianStroke { get; set; }

        /// <summary>
        /// Gets or sets the fill color for the mean marker.
        /// </summary>
        [Parameter]
        public string? MeanFill { get; set; }

        /// <summary>
        /// Gets or sets the width of lines in pixels.
        /// </summary>
        [Parameter]
        public double StrokeWidth { get; set; } = 1;

        /// <summary>
        /// Gets or sets the width of each box in pixels. Auto-calculated if null.
        /// </summary>
        [Parameter]
        public double? Width { get; set; }

        /// <summary>
        /// Gets or sets the width of whisker cap lines in pixels. Defaults to 60% of box width.
        /// </summary>
        [Parameter]
        public double? WhiskerWidth { get; set; }

        /// <summary>
        /// Gets or sets whether to display the mean marker.
        /// </summary>
        [Parameter]
        public bool ShowMean { get; set; }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.TryGetValue<string>(nameof(MedianProperty), out var median) && median != MedianProperty)
            {
                ValueProperty = median;
            }

            await base.SetParametersAsync(parameters);
        }

        /// <inheritdoc />
        public override string Color => Fill ?? string.Empty;

        internal Func<TItem, double> LowerWhisker
        {
            get
            {
                if (string.IsNullOrEmpty(LowerWhiskerProperty))
                {
                    throw new ArgumentException("LowerWhiskerProperty should not be empty");
                }

                return PropertyAccess.Getter<TItem, double>(LowerWhiskerProperty);
            }
        }

        internal Func<TItem, double> LowerQuartile
        {
            get
            {
                if (string.IsNullOrEmpty(LowerQuartileProperty))
                {
                    throw new ArgumentException("LowerQuartileProperty should not be empty");
                }

                return PropertyAccess.Getter<TItem, double>(LowerQuartileProperty);
            }
        }

        internal Func<TItem, double> Median
        {
            get
            {
                if (string.IsNullOrEmpty(MedianProperty))
                {
                    throw new ArgumentException("MedianProperty should not be empty");
                }

                return PropertyAccess.Getter<TItem, double>(MedianProperty);
            }
        }

        internal Func<TItem, double> UpperQuartile
        {
            get
            {
                if (string.IsNullOrEmpty(UpperQuartileProperty))
                {
                    throw new ArgumentException("UpperQuartileProperty should not be empty");
                }

                return PropertyAccess.Getter<TItem, double>(UpperQuartileProperty);
            }
        }

        internal Func<TItem, double> UpperWhisker
        {
            get
            {
                if (string.IsNullOrEmpty(UpperWhiskerProperty))
                {
                    throw new ArgumentException("UpperWhiskerProperty should not be empty");
                }

                return PropertyAccess.Getter<TItem, double>(UpperWhiskerProperty);
            }
        }

        internal Func<TItem, double>? Mean
        {
            get
            {
                if (string.IsNullOrEmpty(MeanProperty))
                {
                    return null;
                }

                return PropertyAccess.Getter<TItem, double>(MeanProperty);
            }
        }

        /// <inheritdoc />
        public override ScaleBase TransformValueScale(ScaleBase scale)
        {
            ArgumentNullException.ThrowIfNull(scale);

            if (Items != null && Items.Any())
            {
                var lw = LowerWhisker;
                var uw = UpperWhisker;

                var minValue = Items.Min(item => lw(item));
                var maxValue = Items.Max(item => uw(item));

                scale.Input.MergeWidth(new ScaleRange { Start = minValue, End = maxValue });
            }

            return scale;
        }

        internal double BoxWidth
        {
            get
            {
                if (Width.HasValue)
                {
                    return Width.Value;
                }

                var chart = RequireChart();
                var availableWidth = chart.CategoryScale.OutputSize - (chart.CategoryAxis.Padding * 2);
                var bands = Items.Count + 2;
                return Math.Max(3, availableWidth / bands * 0.6);
            }
        }

        internal double CapWidth
        {
            get
            {
                if (WhiskerWidth.HasValue)
                {
                    return WhiskerWidth.Value;
                }

                return BoxWidth * 0.6;
            }
        }

        private string FormatValue(double v)
        {
            var chart = RequireChart();
            return chart.ValueAxis.Format(chart.ValueScale, chart.ValueScale.Value(v));
        }

        /// <inheritdoc />
        protected override string TooltipValue(TItem item)
        {
            return FormatValue(Median(item));
        }

        /// <inheritdoc />
        public override RenderFragment RenderTooltip(object data)
        {
            var chart = RequireChart();
            var item = (TItem)data;

            if (TooltipTemplate != null)
            {
                return base.RenderTooltip(data);
            }

            return builder =>
            {
                builder.OpenComponent<Rendering.ChartTooltip>(0);
                builder.AddAttribute(1, nameof(Rendering.ChartTooltip.Class), TooltipClass(item));
                builder.AddAttribute(2, nameof(Rendering.ChartTooltip.Style), TooltipStyle(item));
                builder.AddAttribute(3, nameof(Rendering.ChartTooltip.ChildContent), (RenderFragment)(b =>
                {
                    b.OpenElement(0, "div");
                    b.AddAttribute(1, "class", "rz-chart-tooltip-title");
                    b.AddContent(2, TooltipTitle(item));
                    b.CloseElement();

                    b.OpenElement(3, "div");
                    b.AddContent(4, $"Max: {FormatValue(UpperWhisker(item))}");
                    b.CloseElement();

                    b.OpenElement(5, "div");
                    b.AddContent(6, $"Q3: {FormatValue(UpperQuartile(item))}");
                    b.CloseElement();

                    b.OpenElement(7, "div");
                    b.AddContent(8, $"Median: {FormatValue(Median(item))}");
                    b.CloseElement();

                    b.OpenElement(9, "div");
                    b.AddContent(10, $"Q1: {FormatValue(LowerQuartile(item))}");
                    b.CloseElement();

                    b.OpenElement(11, "div");
                    b.AddContent(12, $"Min: {FormatValue(LowerWhisker(item))}");
                    b.CloseElement();

                    var mean = Mean;
                    if (ShowMean && mean != null)
                    {
                        b.OpenElement(13, "div");
                        b.AddContent(14, $"Mean: {FormatValue(mean(item))}");
                        b.CloseElement();
                    }
                }));
                builder.CloseComponent();
            };
        }

        /// <inheritdoc />
        protected override string TooltipStyle(TItem item)
        {
            var style = base.TooltipStyle(item);

            if (Fill != null)
            {
                return $"{style}; border-color: {Fill};";
            }

            return style;
        }

        /// <inheritdoc />
        internal override double TooltipY(TItem item)
        {
            var chart = RequireChart();
            return chart.ValueScale.Scale(UpperWhisker(item), true);
        }

        /// <inheritdoc />
        public override bool Contains(double x, double y, double tolerance)
        {
            return DataAt(x, y).Item1 != null;
        }

        /// <inheritdoc />
        public override (object, Point) DataAt(double x, double y)
        {
            var chart = Chart;
            if (chart == null || !Items.Any())
            {
                return (default!, new Point());
            }

            var category = ComposeCategory(chart.CategoryScale);
            var boxWidth = BoxWidth;

            foreach (var data in Items)
            {
                var cx = category(data);
                var halfWidth = boxWidth / 2;

                if (x >= cx - halfWidth && x <= cx + halfWidth)
                {
                    var uwY = chart.ValueScale.Scale(UpperWhisker(data), true);
                    var lwY = chart.ValueScale.Scale(LowerWhisker(data), true);
                    var startY = Math.Min(uwY, lwY);
                    var endY = Math.Max(uwY, lwY);

                    if (y >= startY && y <= endY)
                    {
                        return (data!, new Point { X = cx, Y = y });
                    }
                }
            }

            return (default!, new Point());
        }
    }
}
