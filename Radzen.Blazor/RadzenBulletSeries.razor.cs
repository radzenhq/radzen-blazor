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
    /// A chart series that displays bullet charts in a RadzenChart.
    /// Each bullet shows a primary measure bar, a comparative/target marker, and qualitative range bands.
    /// Renders horizontally like <see cref="RadzenBarSeries{TItem}"/>.
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series.</typeparam>
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2026, Justification = TrimMessages.DataTypePreserved)]
    public partial class RadzenBulletSeries<TItem> : CartesianSeries<TItem>
    {
        /// <summary>
        /// Gets or sets the name of the property that provides the target/comparative value.
        /// Rendered as a vertical marker line.
        /// </summary>
        [Parameter]
        public string? TargetProperty { get; set; }

        /// <summary>
        /// Gets or sets the name of the property that provides the maximum range value.
        /// This defines the full extent of the qualitative range background.
        /// </summary>
        [Parameter]
        public string? MaxProperty { get; set; }

        /// <summary>
        /// Gets or sets the fill color for the primary measure bar.
        /// </summary>
        [Parameter]
        public string? Fill { get; set; }

        /// <summary>
        /// Gets or sets the stroke color.
        /// </summary>
        [Parameter]
        public string? Stroke { get; set; }

        /// <summary>
        /// Gets or sets the color of the target marker line.
        /// </summary>
        [Parameter]
        public string? TargetStroke { get; set; }

        /// <summary>
        /// Gets or sets the width of the target marker line in pixels.
        /// </summary>
        [Parameter]
        public double TargetWidth { get; set; } = 2;

        /// <summary>
        /// Gets or sets the stroke width in pixels.
        /// </summary>
        [Parameter]
        public double StrokeWidth { get; set; } = 0;

        /// <summary>
        /// Gets or sets the qualitative range colors from darkest (poor) to lightest (good).
        /// Defaults to three shades of gray.
        /// </summary>
        [Parameter]
        public IEnumerable<string>? RangeColors { get; set; }

        /// <summary>
        /// Gets or sets the qualitative range thresholds as fractions of MaxProperty (e.g. 0.3, 0.6, 1.0).
        /// Must have the same count as RangeColors. Defaults to thirds.
        /// </summary>
        [Parameter]
        public IEnumerable<double>? RangeThresholds { get; set; }

        /// <summary>
        /// Gets or sets the height of each bullet row in pixels. Auto-calculated if null.
        /// </summary>
        [Parameter]
        public double? Height { get; set; }

        /// <inheritdoc />
        public override string Color => Fill ?? string.Empty;

        internal Func<TItem, double> Target
        {
            get
            {
                if (string.IsNullOrEmpty(TargetProperty))
                {
                    throw new ArgumentException("TargetProperty should not be empty");
                }

                return PropertyAccess.Getter<TItem, double>(TargetProperty);
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

        // Swap category and value scales (like RadzenBarSeries) for horizontal rendering
        /// <inheritdoc />
        public override ScaleBase TransformCategoryScale(ScaleBase scale)
        {
            ArgumentNullException.ThrowIfNull(scale);

            // Merge value range into the "category" scale (which becomes the X-axis)
            if (Items != null && Items.Any())
            {
                var max = Max;
                var value = Value;

                var maxVal = Items.Max(item => Math.Max(max(item), value(item)));
                scale.Input.MergeWidth(new ScaleRange { Start = 0, End = maxVal });
            }

            return scale;
        }

        /// <inheritdoc />
        public override ScaleBase TransformValueScale(ScaleBase scale)
        {
            ArgumentNullException.ThrowIfNull(scale);

            // Category data goes into the "value" scale (which becomes the Y-axis)
            return base.TransformCategoryScale(scale);
        }

        /// <inheritdoc />
        protected override IList<object> GetCategories()
        {
            return base.GetCategories().Reverse().ToList();
        }

        internal double BarHeight
        {
            get
            {
                if (Height.HasValue)
                {
                    return Height.Value;
                }

                var chart = RequireChart();
                var availableHeight = chart.ValueScale.OutputSize;
                var bands = Items.Count + 2;
                return Math.Max(10, availableHeight / bands);
            }
        }

        private string FormatValue(double v)
        {
            var chart = RequireChart();
            return chart.ValueAxis.Format(chart.CategoryScale, chart.CategoryScale.Value(v));
        }

        /// <inheritdoc />
        protected override string TooltipValue(TItem item)
        {
            return FormatValue(Value(item));
        }

        /// <inheritdoc />
        protected override string TooltipTitle(TItem item)
        {
            var chart = Chart;
            if (chart == null)
            {
                return string.Empty;
            }

            var category = Category(chart.ValueScale);
            return chart.CategoryAxis.Format(chart.ValueScale, chart.ValueScale.Value(category(item)));
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
                    b.AddContent(4, $"Value: {FormatValue(Value(item))}");
                    b.CloseElement();

                    b.OpenElement(5, "div");
                    b.AddContent(6, $"Target: {FormatValue(Target(item))}");
                    b.CloseElement();
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
        internal override double TooltipX(TItem item)
        {
            var chart = RequireChart();
            return chart.CategoryScale.Compose(Value)(item);
        }

        /// <inheritdoc />
        internal override double TooltipY(TItem item)
        {
            var chart = RequireChart();
            var category = ComposeCategory(chart.ValueScale);
            return category(item);
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

            var category = ComposeCategory(chart.ValueScale);
            var barHeight = BarHeight;
            var halfHeight = barHeight / 2;

            foreach (var data in Items)
            {
                var cy = category(data);

                if (y >= cy - halfHeight && y <= cy + halfHeight)
                {
                    var x0 = chart.CategoryScale.Scale(0, true);
                    var xMax = chart.CategoryScale.Scale(Max(data), true);
                    var startX = Math.Min(x0, xMax);
                    var endX = Math.Max(x0, xMax);

                    if (x >= startX && x <= endX)
                    {
                        return (data!, new Point { X = x, Y = cy });
                    }
                }
            }

            return (default!, new Point());
        }

        internal IList<string> GetRangeColors()
        {
            if (RangeColors != null)
            {
                return RangeColors.ToList();
            }

            return new List<string> { "var(--rz-base-400)", "var(--rz-base-300)", "var(--rz-base-200)" };
        }

        internal IList<double> GetRangeThresholds()
        {
            if (RangeThresholds != null)
            {
                return RangeThresholds.ToList();
            }

            return new List<double> { 0.3, 0.6, 1.0 };
        }
    }
}
