using System;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// A chart series that displays data as a continuous line connecting data points in a RadzenChart.
    /// RadzenLineSeries is ideal for showing trends over time, continuous data, or comparing multiple data series.
    /// Connects data points with lines, making it easy to visualize trends and patterns.
    /// Supports multiple interpolation modes (straight lines, smooth curves, step functions), customizable appearance (color, width, line style),
    /// markers at data points, data labels, combination of multiple line series in one chart for comparison, and line styling with different patterns (solid, dashed, dotted).
    /// Use CategoryProperty for the X-axis values and ValueProperty for the Y-axis values. Enable Smooth for curved lines, or use Interpolation for more control over line rendering.
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series. Each item represents one point on the line.</typeparam>
    /// <example>
    /// Basic line series:
    /// <code>
    /// &lt;RadzenChart&gt;
    ///     &lt;RadzenLineSeries Data=@temperatures CategoryProperty="Date" ValueProperty="Temperature" Title="Temperature" /&gt;
    /// &lt;/RadzenChart&gt;
    /// </code>
    /// Smooth line with markers:
    /// <code>
    /// &lt;RadzenChart&gt;
    ///     &lt;RadzenLineSeries Data=@data CategoryProperty="X" ValueProperty="Y" Smooth="true" Stroke="#FF6384" StrokeWidth="3"&gt;
    ///         &lt;RadzenMarkers MarkerType="MarkerType.Circle" /&gt;
    ///         &lt;RadzenSeriesDataLabels Visible="true" /&gt;
    ///     &lt;/RadzenLineSeries&gt;
    /// &lt;/RadzenChart&gt;
    /// </code>
    /// </example>
    public partial class RadzenLineSeries<TItem> : CartesianSeries<TItem>
    {
        /// <summary>
        /// Gets or sets the color of the line.
        /// Supports any valid CSS color value (e.g., "#FF0000", "rgb(255,0,0)", "var(--my-color)").
        /// If not set, uses the color from the chart's color scheme.
        /// </summary>
        /// <value>The line color as a CSS color value.</value>
        [Parameter]
        public string Stroke { get; set; }

        /// <summary>
        /// Gets or sets the width of the line in pixels.
        /// Thicker lines are more visible but may obscure details in dense data.
        /// </summary>
        /// <value>The line width in pixels. Default is 2.</value>
        [Parameter]
        public double StrokeWidth { get; set; } = 2;

        /// <summary>
        /// Gets or sets the line style pattern (solid, dashed, dotted).
        /// Use LineType.Dashed or LineType.Dotted to create non-solid lines for visual distinction or to represent projected/estimated data.
        /// </summary>
        /// <value>The line style. Default is solid.</value>
        [Parameter]
        public LineType LineType { get; set; }

        /// <summary>
        /// Gets or sets whether to render smooth curved lines between data points instead of straight lines.
        /// When true, uses spline interpolation to create smooth curves. This is a convenience property for setting <see cref="Interpolation"/> to Spline.
        /// </summary>
        /// <value><c>true</c> for smooth curved lines; <c>false</c> for straight lines. Default is <c>false</c>.</value>
        [Parameter]
        public bool Smooth
        {
            get => Interpolation == Interpolation.Spline;
            set => Interpolation = value ? Interpolation.Spline : Interpolation.Line;
        }

        /// <summary>
        /// Gets or sets the interpolation method used to render lines between data points.
        /// Options include Line (straight lines), Spline (smooth curves), and Step (stair-step lines).
        /// </summary>
        /// <value>The interpolation method. Default is <see cref="Interpolation.Line"/>.</value>
        [Parameter]
        public Interpolation Interpolation { get; set; } = Interpolation.Line;

        /// <inheritdoc />
        public override string Color
        {
            get
            {
                return Stroke;
            }
        }

        /// <inheritdoc />
        protected override string TooltipStyle(TItem item)
        {
            var style = base.TooltipStyle(item);

            var index = Items.IndexOf(item);

            if (index >= 0)
            {
                var color = Stroke;

                if (color != null)
                {
                    style = $"{style}; border-color: {color};";
                }
            }

            return style;
        }

        /// <inheritdoc />
        public override bool Contains(double x, double y, double tolerance)
        {
            var category = ComposeCategory(Chart.CategoryScale);
            var value = ComposeValue(Chart.ValueScale);

            var points = Items.Select(item => new Point { X = category(item), Y = value(item) }).ToArray();

            if (points.Length > 0)
            {
                if (points.Length == 1)
                {
                    var point = points[0];

                    var polygon = new[] {
                        new Point { X = point.X - tolerance, Y = point.Y - tolerance },
                        new Point { X = point.X - tolerance, Y = point.Y + tolerance },
                        new Point { X = point.X + tolerance, Y = point.Y + tolerance },
                        new Point { X = point.X + tolerance, Y = point.Y - tolerance },
                    };

                    if (InsidePolygon(new Point { X = x, Y = y }, polygon))
                    {
                        return true;
                    }
                }
                else
                {
                    for (var i = 0; i < points.Length - 1; i++)
                    {
                        var start = points[i];
                        var end = points[i + 1];

                        var polygon = new[] {
                            new Point { X = start.X, Y = start.Y - tolerance },
                            new Point { X = end.X, Y = end.Y - tolerance },
                            new Point { X = end.X, Y = end.Y + tolerance },
                            new Point { X = start.X, Y = start.Y + tolerance },
                        };

                        if (InsidePolygon(new Point { X = x, Y = y }, polygon))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <inheritdoc />
        public override IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY)
        {
            return base.GetDataLabels(offsetX, offsetY - 16);
        }

        private IPathGenerator GetPathGenerator()
        {
            switch(Interpolation)
            {
                case Interpolation.Line:
                    return new LineGenerator();
                case Interpolation.Spline:
                    return new SplineGenerator();
                case Interpolation.Step:
                    return new StepGenerator();
                default:
                    throw new NotSupportedException($"Interpolation {Interpolation} is not supported yet.");
            }
        }
    }
}
