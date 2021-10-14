using Microsoft.AspNetCore.Components;
using System;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// Renders area series in <see cref="RadzenChart" />.
    /// </summary>
    public partial class RadzenAreaSeries<TItem> : CartesianSeries<TItem>
    {
        /// <summary>
        /// Specifies the color of the line.
        /// </summary>
        /// <value>The stroke.</value>
        [Parameter]
        public string Stroke { get; set; }

        /// <summary>
        /// Specifies the fill (background color) of the area series.
        /// </summary>
        /// <value>The fill.</value>
        [Parameter]
        public string Fill { get; set; }

        /// <summary>
        /// Gets or sets the pixel width of the line. Set to <c>2</c> by default.
        /// </summary>
        /// <value>The width of the stroke.</value>
        [Parameter]
        public double StrokeWidth { get; set; } = 2;

        /// <summary>
        /// Specifies the line type.
        /// </summary>
        [Parameter]
        public LineType LineType { get; set; }

        /// <summary>
        /// Specifies whether to render a smooth line. Set to <c>false</c> by default.
        /// </summary>
        [Parameter]
        public bool Smooth { get; set; }

        /// <inheritdoc />
        public override string Color
        {
            get
            {
                return Stroke;
            }
        }

        /// <inheritdoc />
        public override bool Contains(double x, double y, double tolerance)
        {
            var category = ComposeCategory(Chart.CategoryScale);
            var value = ComposeValue(Chart.ValueScale);

            var points = Items.Select(item => new Point { X = category(item), Y = value(item) }).ToArray();

            var valueTicks = Chart.ValueScale.Ticks(Chart.ValueAxis.TickDistance);
            var axisY = Chart.ValueScale.Scale(Math.Max(0, valueTicks.Start));

            if (points.Any())
            {
                for (var i = 0; i < points.Length - 1; i++)
                {
                    var start = points[i];
                    var end = points[i + 1];

                    var polygon = new[]
                    {
                        new Point { X = start.X, Y = start.Y - tolerance },
                        new Point { X = end.X, Y = end.Y - tolerance },
                        new Point { X = end.X, Y = axisY },
                        new Point { X = start.X, Y = axisY },
                    };

                    if (InsidePolygon(new Point { X = x, Y = y }, polygon))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}