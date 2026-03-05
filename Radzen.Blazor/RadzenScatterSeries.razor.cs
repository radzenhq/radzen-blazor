using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// A chart series that renders individual data points at numeric X/Y coordinates without connecting lines.
    /// Ideal for visualizing correlations between two numeric variables (e.g., height vs weight, price vs quantity).
    /// Both <see cref="CartesianSeries{TItem}.CategoryProperty"/> and <see cref="CartesianSeries{TItem}.ValueProperty"/> must be numeric.
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenChart&gt;
    ///     &lt;RadzenScatterSeries Data=@data CategoryProperty="Weight" ValueProperty="Height" Title="People" /&gt;
    ///     &lt;RadzenCategoryAxis /&gt;
    ///     &lt;RadzenValueAxis /&gt;
    /// &lt;/RadzenChart&gt;
    /// </code>
    /// </example>
    public partial class RadzenScatterSeries<TItem> : CartesianSeries<TItem>
    {
        /// <summary>
        /// Gets or sets the marker outline color.
        /// If not set, uses the color from the chart's color scheme.
        /// </summary>
        [Parameter]
        public string? Stroke { get; set; }

        /// <summary>
        /// Gets or sets the marker fill color.
        /// If not set, uses the color from the chart's color scheme.
        /// </summary>
        [Parameter]
        public string? Fill { get; set; }

        /// <summary>
        /// Gets or sets the marker outline width in pixels.
        /// </summary>
        [Parameter]
        public double StrokeWidth { get; set; }

        /// <inheritdoc />
        public override string Color => Fill ?? Stroke ?? string.Empty;

        /// <inheritdoc />
        public override MarkerType MarkerType
        {
            get
            {
                var markerType = MarkerType.None;

                if (Markers != null)
                {
                    markerType = Markers.MarkerType;
                }

                if (markerType == MarkerType.None || markerType == MarkerType.Auto)
                {
                    if (Chart != null)
                    {
                        var index = Chart.Series.IndexOf(this);
                        var types = new[] { MarkerType.Circle, MarkerType.Triangle, MarkerType.Square, MarkerType.Diamond };
                        markerType = types[index % types.Length];
                    }
                    else
                    {
                        markerType = MarkerType.Circle;
                    }
                }

                return markerType;
            }
        }

        /// <inheritdoc />
        public override bool Contains(double x, double y, double tolerance)
        {
            var chart = Chart;
            if (chart == null || !Items.Any())
            {
                return false;
            }

            var category = ComposeCategory(chart.CategoryScale);
            var value = ComposeValue(chart.ValueScale);

            return Items.Any(item =>
            {
                var px = category(item);
                var py = value(item);
                var dx = px - x;
                var dy = py - y;
                return Math.Sqrt(dx * dx + dy * dy) <= tolerance;
            });
        }

        /// <inheritdoc />
        public override (object, Point) DataAt(double x, double y)
        {
            if (!Items.Any())
            {
                return (default!, new Point());
            }

            var result = Items.Select(item =>
            {
                var px = TooltipX(item);
                var py = TooltipY(item);
                var dx = px - x;
                var dy = py - y;
                return new { Item = item, Distance = Math.Sqrt(dx * dx + dy * dy), Point = new Point { X = px, Y = py } };
            }).Aggregate((a, b) => a.Distance < b.Distance ? a : b);

            // Don't capture when the mouse is very far from all data points
            // (e.g., hovering over axis labels or chart margins).
            if (result.Distance > 100)
            {
                return (default!, new Point());
            }

            var markerRadius = MarkerSize;
            if (result.Distance <= markerRadius)
            {
                return (result.Item!, result.Point);
            }

            // Return a position clamped to within markerRadius of the mouse so the
            // chart's tolerance check stays satisfied across dead zones between points.
            // Without this, the tooltip close/open animation cycle causes visible flicker.
            // Actual tooltip placement is handled by GetTooltipPosition independently.
            var scale = markerRadius / result.Distance;
            return (result.Item!, new Point
            {
                X = x + (result.Point.X - x) * scale,
                Y = y + (result.Point.Y - y) * scale
            });
        }

        /// <inheritdoc />
        protected override string TooltipStyle(TItem item)
        {
            var style = base.TooltipStyle(item);
            var color = Fill ?? Stroke;
            if (color != null)
            {
                style = $"{style}; border-color: {color};";
            }
            return style;
        }

        /// <inheritdoc />
        protected override string TooltipValue(TItem item)
        {
            var chart = RequireChart();
            return chart.ValueAxis.Format(chart.ValueScale, chart.ValueScale.Value(Value(item)));
        }

        /// <inheritdoc />
        public override IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY)
        {
            return base.GetDataLabels(offsetX, offsetY - 16);
        }
    }
}
