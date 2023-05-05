using System;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// Renders line series in <see cref="RadzenChart" />.
    /// </summary>
    /// <typeparam name="TItem">The type of the series data item.</typeparam>
    public partial class RadzenLineSeries<TItem> : CartesianSeries<TItem>
    {
        /// <summary>
        /// Specifies the color of the line.
        /// </summary>
        /// <value>The stroke.</value>
        [Parameter]
        public string Stroke { get; set; }

        /// <summary>
        /// Specifies the pixel width of the line. Set to <c>2</c> by default.
        /// </summary>
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
        public bool Smooth
        {
            get => Interpolation == Interpolation.Spline;
            set => Interpolation = value ? Interpolation.Spline : Interpolation.Line;
        }

        /// <summary>
        /// Specifies how to render lines between data points. Set to <see cref="Line"/> by default
        /// </summary>
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
