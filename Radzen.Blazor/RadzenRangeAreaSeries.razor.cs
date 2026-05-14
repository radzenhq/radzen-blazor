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
    /// A chart series that displays a filled area between two value lines in a RadzenChart.
    /// The area spans from <see cref="MinProperty"/> to <see cref="MaxProperty"/> for each category,
    /// useful for showing ranges like temperature bands or confidence intervals.
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenChart&gt;
    ///     &lt;RadzenRangeAreaSeries Data=@data CategoryProperty="Month"
    ///         MinProperty="Low" MaxProperty="High" Title="Temperature Range" /&gt;
    /// &lt;/RadzenChart&gt;
    /// </code>
    /// </example>
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2026, Justification = TrimMessages.DataTypePreserved)]
    public partial class RadzenRangeAreaSeries<TItem> : CartesianSeries<TItem>
    {
        /// <summary>
        /// Gets or sets the name of the property that provides the minimum (lower) value.
        /// </summary>
        [Parameter]
        public string? MinProperty { get; set; }

        /// <summary>
        /// Gets or sets the name of the property that provides the maximum (upper) value.
        /// </summary>
        [Parameter]
        public string? MaxProperty { get; set; }

        /// <summary>
        /// Gets or sets the stroke color of the upper and lower boundary lines.
        /// </summary>
        [Parameter]
        public string? Stroke { get; set; }

        /// <summary>
        /// Gets or sets the fill color of the area between min and max lines.
        /// </summary>
        [Parameter]
        public string? Fill { get; set; }

        /// <summary>
        /// Gets or sets the pixel width of the boundary lines.
        /// </summary>
        [Parameter]
        public double StrokeWidth { get; set; } = 2;

        /// <summary>
        /// Gets or sets the line type for the boundary lines.
        /// </summary>
        [Parameter]
        public LineType LineType { get; set; }

        /// <summary>
        /// Specifies whether to render smooth lines. Set to <c>false</c> by default.
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
        public override string Color => Stroke ?? string.Empty;

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.TryGetValue<string>(nameof(MaxProperty), out var max) && max != MaxProperty)
            {
                ValueProperty = max;
            }

            await base.SetParametersAsync(parameters);
        }

        internal Func<TItem, double> Min
        {
            get
            {
                if (string.IsNullOrEmpty(MinProperty))
                {
                    throw new ArgumentException("MinProperty should not be empty");
                }

                return PropertyAccess.Getter<TItem, double>(MinProperty);
            }
        }

        internal Func<TItem, double> Max
        {
            get
            {
                if (string.IsNullOrEmpty(MaxProperty))
                {
                    throw new ArgumentException("MaxProperty should not be empty");
                }

                return PropertyAccess.Getter<TItem, double>(MaxProperty);
            }
        }

        /// <inheritdoc />
        public override ScaleBase TransformValueScale(ScaleBase scale)
        {
            ArgumentNullException.ThrowIfNull(scale);

            if (Items != null && Items.Any())
            {
                var min = Min;
                var max = Max;

                var minValue = Items.Min(item => min(item));
                var maxValue = Items.Max(item => max(item));

                scale.Input.MergeWidth(new ScaleRange { Start = minValue, End = maxValue });
            }

            return scale;
        }

        /// <inheritdoc />
        protected override string TooltipStyle(TItem item)
        {
            var style = base.TooltipStyle(item);

            if (Fill != null)
            {
                style = $"{style}; border-color: {Fill};";
            }

            return style;
        }

        /// <inheritdoc />
        protected override string TooltipValue(TItem item)
        {
            var chart = RequireChart();
            var minVal = chart.ValueAxis.Format(chart.ValueScale, chart.ValueScale.Value(Min(item)));
            var maxVal = chart.ValueAxis.Format(chart.ValueScale, chart.ValueScale.Value(Max(item)));
            return $"{minVal} - {maxVal}";
        }

        /// <inheritdoc />
        internal override double TooltipY(TItem item)
        {
            var chart = RequireChart();
            var minY = chart.ValueScale.Scale(Min(item), true);
            var maxY = chart.ValueScale.Scale(Max(item), true);
            return (minY + maxY) / 2;
        }

        /// <inheritdoc />
        public override bool Contains(double x, double y, double tolerance)
        {
            var chart = Chart;
            if (chart == null)
            {
                return false;
            }

            var category = ComposeCategory(chart.CategoryScale);

            var min = Min;
            var max = Max;

            var points = Items.Select(item => new { X = category(item), MinY = chart.ValueScale.Scale(min(item), true), MaxY = chart.ValueScale.Scale(max(item), true) }).ToArray();

            if (points.Length > 1)
            {
                for (var i = 0; i < points.Length - 1; i++)
                {
                    var start = points[i];
                    var end = points[i + 1];

                    var polygon = new[]
                    {
                        new Point { X = start.X, Y = start.MaxY },
                        new Point { X = end.X, Y = end.MaxY },
                        new Point { X = end.X, Y = end.MinY },
                        new Point { X = start.X, Y = start.MinY },
                    };

                    if (InsidePolygon(new Point { X = x, Y = y }, polygon))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal IPathGenerator GetPathGenerator()
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
    }
}
