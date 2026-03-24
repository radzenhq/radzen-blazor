using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A chart series that displays data as vertical high-low lines in a <see cref="RadzenChart"/>.
    /// Each data point renders a vertical line from the low value to the high value,
    /// similar to a simplified candlestick chart without open and close values.
    /// This is useful for showing value ranges such as daily temperature ranges or price ranges.
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series. Each item represents one high-low line.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenChart&gt;
    ///     &lt;RadzenHighLowSeries Data=@data CategoryProperty="Date"
    ///         HighProperty="High" LowProperty="Low" Title="Price Range" /&gt;
    ///     &lt;RadzenCategoryAxis FormatString="{0:MM/dd}" /&gt;
    /// &lt;/RadzenChart&gt;
    /// </code>
    /// </example>
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2026, Justification = TrimMessages.DataTypePreserved)]
    public partial class RadzenHighLowSeries<TItem> : CartesianSeries<TItem>
    {
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
        /// Gets or sets the stroke (line) color.
        /// Supports any valid CSS color value (e.g., "#FF0000", "rgb(255,0,0)").
        /// If not set, the color is determined by the chart's theme color scheme.
        /// </summary>
        /// <value>The line color as a CSS color value, or <c>null</c> to use the theme default.</value>
        [Parameter]
        public string? Stroke { get; set; }

        /// <summary>
        /// Gets or sets the width of the high-low lines in pixels.
        /// </summary>
        /// <value>The stroke width in pixels. Default is 2.</value>
        [Parameter]
        public double StrokeWidth { get; set; } = 2;

        /// <summary>
        /// Gets or sets the line style pattern (solid, dashed, dotted).
        /// Use <see cref="Blazor.LineType.Dashed"/> or <see cref="Blazor.LineType.Dotted"/> for non-solid lines.
        /// </summary>
        /// <value>The line style. Default is solid.</value>
        [Parameter]
        public LineType LineType { get; set; }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            // Sync ValueProperty from HighProperty so base class methods work correctly.
            if (parameters.TryGetValue<string>(nameof(HighProperty), out var high) && high != HighProperty)
            {
                ValueProperty = high;
            }

            await base.SetParametersAsync(parameters);
        }

        /// <inheritdoc />
        public override string Color => Stroke ?? string.Empty;

        /// <summary>
        /// Gets a delegate that returns the High value from a data item.
        /// </summary>
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

        /// <summary>
        /// Gets a delegate that returns the Low value from a data item.
        /// </summary>
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

        private string FormatValue(double v)
        {
            var chart = RequireChart();
            return chart.ValueAxis.Format(chart.ValueScale, chart.ValueScale.Value(v));
        }

        /// <inheritdoc />
        protected override string TooltipValue(TItem item)
        {
            return $"{FormatValue(Low(item))} - {FormatValue(High(item))}";
        }

        /// <inheritdoc />
        protected override string TooltipStyle(TItem item)
        {
            var style = base.TooltipStyle(item);

            if (Stroke != null)
            {
                return $"{style}; border-color: {Stroke};";
            }

            return style;
        }

        /// <inheritdoc />
        internal override double TooltipY(TItem item)
        {
            var chart = RequireChart();
            var highY = chart.ValueScale.Scale(High(item), true);
            var lowY = chart.ValueScale.Scale(Low(item), true);

            // Position tooltip at the midpoint of the high-low line.
            return (highY + lowY) / 2;
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

            foreach (var data in Items)
            {
                var cx = category(data);

                if (x >= cx - StrokeWidth && x <= cx + StrokeWidth)
                {
                    var highY = chart.ValueScale.Scale(High(data), true);
                    var lowY = chart.ValueScale.Scale(Low(data), true);
                    var startY = Math.Min(highY, lowY);
                    var endY = Math.Max(highY, lowY);

                    if (y >= startY - StrokeWidth && y <= endY + StrokeWidth)
                    {
                        return (data!, new Point { X = cx, Y = y });
                    }
                }
            }

            return (default!, new Point());
        }
    }
}
