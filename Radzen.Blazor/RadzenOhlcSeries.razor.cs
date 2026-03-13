using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A chart series that displays financial data as OHLC (Open-High-Low-Close) bars in a RadzenChart.
    /// Each bar shows a vertical line from High to Low, with a left tick at the Open price and a right tick at the Close price.
    /// Similar to <see cref="RadzenCandlestickSeries{TItem}"/> but uses tick marks instead of filled bodies.
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series. Each item represents one OHLC bar.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenChart&gt;
    ///     &lt;RadzenOhlcSeries Data=@stockData CategoryProperty="Date"
    ///         OpenProperty="Open" HighProperty="High" LowProperty="Low" CloseProperty="Close"
    ///         Title="AAPL" /&gt;
    ///     &lt;RadzenCategoryAxis FormatString="{0:MM/dd}" /&gt;
    /// &lt;/RadzenChart&gt;
    /// </code>
    /// </example>
    public partial class RadzenOhlcSeries<TItem> : CartesianSeries<TItem>
    {
        /// <summary>
        /// Gets or sets the name of the property of <typeparamref name="TItem"/> that provides the Open value.
        /// </summary>
        [Parameter]
        public string? OpenProperty { get; set; }

        /// <summary>
        /// Gets or sets the name of the property of <typeparamref name="TItem"/> that provides the High value.
        /// </summary>
        [Parameter]
        public string? HighProperty { get; set; }

        /// <summary>
        /// Gets or sets the name of the property of <typeparamref name="TItem"/> that provides the Low value.
        /// </summary>
        [Parameter]
        public string? LowProperty { get; set; }

        /// <summary>
        /// Gets or sets the name of the property of <typeparamref name="TItem"/> that provides the Close value.
        /// </summary>
        [Parameter]
        public string? CloseProperty { get; set; }

        /// <summary>
        /// Gets or sets the stroke color for bullish bars (Close &gt;= Open).
        /// When not set, the theme color scheme applies.
        /// </summary>
        [Parameter]
        public string? BullStroke { get; set; }

        /// <summary>
        /// Gets or sets the stroke color for bearish bars (Close &lt; Open).
        /// When not set, the theme color scheme applies.
        /// </summary>
        [Parameter]
        public string? BearStroke { get; set; }

        /// <summary>
        /// Gets or sets the width of the OHLC bar lines in pixels.
        /// </summary>
        /// <value>The stroke width in pixels. Default is 2.</value>
        [Parameter]
        public double StrokeWidth { get; set; } = 2;

        /// <summary>
        /// Gets or sets the width of the open/close tick marks in pixels.
        /// If null, the width is calculated automatically based on the chart size and number of data points.
        /// </summary>
        [Parameter]
        public double? TickWidth { get; set; }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.TryGetValue<string>(nameof(CloseProperty), out var close) && close != CloseProperty)
            {
                ValueProperty = close;
            }

            await base.SetParametersAsync(parameters);
        }

        /// <inheritdoc />
        public override string Color => BullStroke ?? string.Empty;

        internal Func<TItem, double> Open
        {
            get
            {
                if (string.IsNullOrEmpty(OpenProperty))
                {
                    throw new ArgumentException("OpenProperty should not be empty");
                }

                return PropertyAccess.Getter<TItem, double>(OpenProperty);
            }
        }

        internal Func<TItem, double> High
        {
            get
            {
                if (string.IsNullOrEmpty(HighProperty))
                {
                    throw new ArgumentException("HighProperty should not be empty");
                }

                return PropertyAccess.Getter<TItem, double>(HighProperty);
            }
        }

        internal Func<TItem, double> Low
        {
            get
            {
                if (string.IsNullOrEmpty(LowProperty))
                {
                    throw new ArgumentException("LowProperty should not be empty");
                }

                return PropertyAccess.Getter<TItem, double>(LowProperty);
            }
        }

        internal Func<TItem, double> Close
        {
            get
            {
                if (string.IsNullOrEmpty(CloseProperty))
                {
                    throw new ArgumentException("CloseProperty should not be empty");
                }

                return PropertyAccess.Getter<TItem, double>(CloseProperty);
            }
        }

        /// <inheritdoc />
        public override ScaleBase TransformValueScale(ScaleBase scale)
        {
            ArgumentNullException.ThrowIfNull(scale);

            if (Items != null && Items.Any())
            {
                var high = High;
                var low = Low;

                var minValue = Items.Min(item => low(item));
                var maxValue = Items.Max(item => high(item));

                scale.Input.MergeWidth(new ScaleRange { Start = minValue, End = maxValue });
            }

            return scale;
        }

        internal double BarTickWidth
        {
            get
            {
                if (TickWidth.HasValue)
                {
                    return TickWidth.Value;
                }

                var chart = RequireChart();
                var availableWidth = chart.CategoryScale.OutputSize - (chart.CategoryAxis.Padding * 2);
                var bands = Items.Count + 2;
                return Math.Max(3, availableWidth / bands * 0.3);
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
            return FormatValue(Close(item));
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
                    b.AddContent(4, $"Open: {FormatValue(Open(item))}");
                    b.CloseElement();

                    b.OpenElement(5, "div");
                    b.AddContent(6, $"High: {FormatValue(High(item))}");
                    b.CloseElement();

                    b.OpenElement(7, "div");
                    b.AddContent(8, $"Low: {FormatValue(Low(item))}");
                    b.CloseElement();

                    b.OpenElement(9, "div");
                    b.AddContent(10, $"Close: {FormatValue(Close(item))}");
                    b.CloseElement();
                }));
                builder.CloseComponent();
            };
        }

        /// <inheritdoc />
        protected override string TooltipStyle(TItem item)
        {
            var style = base.TooltipStyle(item);
            var isBull = Close(item) >= Open(item);
            var color = isBull ? BullStroke : BearStroke;

            if (color != null)
            {
                return $"{style}; border-color: {color};";
            }

            return style;
        }

        /// <inheritdoc />
        internal override double TooltipY(TItem item)
        {
            var chart = RequireChart();
            return chart.ValueScale.Scale(High(item), true);
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
            var tickWidth = BarTickWidth;

            foreach (var data in Items)
            {
                var cx = category(data);

                if (x >= cx - tickWidth && x <= cx + tickWidth)
                {
                    var highY = chart.ValueScale.Scale(High(data), true);
                    var lowY = chart.ValueScale.Scale(Low(data), true);
                    var startY = Math.Min(highY, lowY);
                    var endY = Math.Max(highY, lowY);

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
