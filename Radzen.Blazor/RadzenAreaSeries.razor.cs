using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
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
        public string? Stroke { get; set; }

        /// <summary>
        /// Specifies the fill (background color) of the area series.
        /// When <see cref="FillMode"/> is <see cref="FillMode.Gradient"/> this color is used as the gradient base color.
        /// </summary>
        /// <value>The fill.</value>
        [Parameter]
        public string? Fill { get; set; }

        /// <summary>
        /// Specifies how the area is filled. Set to <see cref="FillMode.Solid"/> by default.
        /// Use <see cref="FillMode.Gradient"/> for a fill that fades toward the baseline, or <see cref="FillMode.None"/> to render only the line.
        /// </summary>
        /// <value>The fill mode. Default is <see cref="FillMode.Solid"/>.</value>
        [Parameter]
        public FillMode FillMode { get; set; } = FillMode.Solid;

        /// <summary>
        /// Specifies the opacity at the top of the gradient fill. Used when <see cref="FillMode"/> is <see cref="FillMode.Gradient"/>.
        /// </summary>
        /// <value>The gradient start opacity. Default is <c>0.55</c>.</value>
        [Parameter]
        public double GradientStartOpacity { get; set; } = 0.55;

        /// <summary>
        /// Specifies the opacity at the baseline of the gradient fill. Used when <see cref="FillMode"/> is <see cref="FillMode.Gradient"/>.
        /// </summary>
        /// <value>The gradient end opacity. Default is <c>0.05</c>.</value>
        [Parameter]
        public double GradientEndOpacity { get; set; } = 0.05;

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
        public bool Smooth
        {
            get => Interpolation == Interpolation.Spline;
            set => Interpolation = value ? Interpolation.Spline : Interpolation.Line;
        }

        /// <summary>
        /// Specifies how to render lines between data points. Set to <see cref="Interpolation.Line"/> by default
        /// </summary>
        [Parameter]
        public Interpolation Interpolation { get; set; } = Interpolation.Line;

        /// <inheritdoc />
        public override string Color
        {
            get
            {
                return Stroke ?? string.Empty;
            }
        }

        /// <inheritdoc />
        protected override string TooltipStyle(TItem item)
        {
            var style = base.TooltipStyle(item);

            var index = Items.IndexOf(item);

            if (index >= 0)
            {
                var color = Fill;

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
            var chart = Chart;
            if (chart == null)
            {
                return false;
            }

            var vs = chart.GetValueScale(ValueAxisName);
            var va = chart.GetValueAxis(ValueAxisName);
            var category = ComposeCategory(chart.CategoryScale);
            var value = ComposeValue(vs);

            var points = Items.Select(item => new Point { X = category(item), Y = value(item) }).ToArray();

            var valueTicks = vs.Ticks(va.TickDistance);
            var axisY = vs.Scale(Math.Max(0, valueTicks.Start));

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