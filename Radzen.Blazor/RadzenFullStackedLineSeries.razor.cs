using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// Renders 100% stacked line series in <see cref="RadzenChart" />.
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series.</typeparam>
    public partial class RadzenFullStackedLineSeries<TItem> : CartesianSeries<TItem>, IChartFullStackedAreaSeries
    {
        /// <summary>
        /// Specifies the color of the line.
        /// </summary>
        /// <value>The stroke.</value>
        [Parameter]
        public string? Stroke { get; set; }

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
        /// Specifies how to render lines between data points. Set to <see cref="Interpolation.Line"/> by default.
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
            if (Chart == null)
            {
                return false;
            }

            var category = ComposeCategory(Chart.CategoryScale);
            var value = ComposeValue(Chart.ValueScale);

            var points = GetTopPoints(category, value, Chart.ValueScale).ToArray();

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

        private IEnumerable<Point<TItem>> GetTopPoints(Func<TItem, double> category, Func<TItem, double> value, ScaleBase valueScale)
        {
            var allSeries = StackedLineSeries;
            var index = allSeries.IndexOf(this);

            return GetPointsAt(index, category, value, valueScale);
        }

        private IEnumerable<Point<TItem>> GetPointsAt(int index, Func<TItem, double> category, Func<TItem, double> value, ScaleBase valueScale)
        {
            var allSeries = StackedLineSeries;
            return Items.Select(item =>
            {
                var x = category(item);

                var cumulativeSum = allSeries.Take(index + 1).SelectMany(series => series.ValuesForCategory(x)).DefaultIfEmpty(value(item)).Sum();
                var total = allSeries.SelectMany(series => series.ValuesForCategory(x)).Select(Math.Abs).Sum();
                var percentage = total != 0 ? (cumulativeSum / total) * 100 : 0;

                var y = valueScale.Scale(percentage);

                return new Point<TItem> { X = x, Y = y, Data = item };
            }).ToList();
        }

        /// <inheritdoc />
        public override IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY)
        {
            return base.GetDataLabels(offsetX, offsetY - 16);
        }

        /// <inheritdoc />
        internal override double TooltipY(TItem item)
        {
            if (Chart == null)
            {
                return 0;
            }

            var category = ComposeCategory(Chart.CategoryScale);
            var value = ComposeValue(Chart.ValueScale);
            var x = category(item);
            var allSeries = StackedLineSeries;
            var index = allSeries.IndexOf(this);

            var cumulativeSum = allSeries.Take(index + 1).SelectMany(series => series.ValuesForCategory(x)).DefaultIfEmpty(value(item)).Sum();
            var total = allSeries.SelectMany(series => series.ValuesForCategory(x)).Select(Math.Abs).Sum();
            var percentage = total != 0 ? (cumulativeSum / total) * 100 : 0;

            var y = Chart.ValueScale.Scale(percentage);
            return y;
        }

        private IPathGenerator GetPathGenerator()
        {
            switch (Interpolation)
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

        private IList<IChartSeries> LineSeries => Chart?.Series?.Where(series => series is IChartFullStackedAreaSeries).Cast<IChartSeries>().ToList() ?? new List<IChartSeries>();
        private IList<IChartSeries> VisibleLineSeries => LineSeries.Where(series => series.Visible).ToList();
        private IList<IChartFullStackedAreaSeries> StackedLineSeries => VisibleLineSeries.Cast<IChartFullStackedAreaSeries>().ToList();

        /// <inheritdoc />
        public override ScaleBase TransformValueScale(ScaleBase scale)
        {
            ArgumentNullException.ThrowIfNull(scale);

            if (Items.Any())
            {
                scale.Input.MergeWidth(new ScaleRange { Start = 0, End = 100 });
            }

            return scale;
        }

        int IChartFullStackedAreaSeries.Count
        {
            get
            {
                if (Items == null)
                {
                    return 0;
                }

                return Items.Count;
            }
        }

        IEnumerable<double> IChartFullStackedAreaSeries.ValuesForCategory(double value)
        {
            if (Items == null)
            {
                return Enumerable.Empty<double>();
            }

            var category = Chart != null ? ComposeCategory(Chart.CategoryScale) : (Func<TItem, double>)(item => 0);

            return Items.Where(item => category(item) == value).Select(Value);
        }

        double IChartFullStackedAreaSeries.ValueAt(int index)
        {
            if (Items == null || index < 0 || index >= Items.Count)
            {
                return 0;
            }

            return Value(Items[index]);
        }
    }
}
