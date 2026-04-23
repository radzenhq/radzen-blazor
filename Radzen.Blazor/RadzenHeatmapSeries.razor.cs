using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// A chart series that renders a grid of colored cells (a heatmap). Each data item provides an
    /// X coordinate (<see cref="CartesianSeries{TItem}.CategoryProperty"/>), a Y coordinate
    /// (<see cref="CartesianSeries{TItem}.ValueProperty"/>) and an intensity value
    /// (<see cref="IntensityProperty"/>) that drives the cell color.
    /// <para>
    /// Cell color is picked from <see cref="ColorRange"/>. When no range matches, <see cref="Fill"/> is used.
    /// Both <see cref="CartesianSeries{TItem}.CategoryProperty"/> and <see cref="CartesianSeries{TItem}.ValueProperty"/>
    /// must be numeric.
    /// </para>
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenChart&gt;
    ///     &lt;RadzenHeatmapSeries Data=@grid CategoryProperty="X" ValueProperty="Y" IntensityProperty="Lux"
    ///         ColorRange="@ranges" Title="Illuminance" /&gt;
    ///     &lt;RadzenCategoryAxis Min="0" Max="5" /&gt;
    ///     &lt;RadzenValueAxis Min="0" Max="5" /&gt;
    /// &lt;/RadzenChart&gt;
    /// </code>
    /// </example>
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2026, Justification = TrimMessages.DataTypePreserved)]
    public partial class RadzenHeatmapSeries<TItem> : CartesianSeries<TItem>
    {
        /// <summary>
        /// The name of the numeric property of <typeparamref name="TItem"/> that provides the cell intensity
        /// (the value mapped to color).
        /// </summary>
        [Parameter]
        public string? IntensityProperty { get; set; }

        /// <summary>
        /// The width of each cell in axis units. When <c>0</c> (default) the width is inferred from the
        /// smallest gap between unique X values in the data.
        /// </summary>
        [Parameter]
        public double CellWidth { get; set; }

        /// <summary>
        /// The height of each cell in axis units. When <c>0</c> (default) the height is inferred from the
        /// smallest gap between unique Y values in the data.
        /// </summary>
        [Parameter]
        public double CellHeight { get; set; }

        /// <summary>
        /// The default fill color applied to cells whose intensity does not match any entry in
        /// <see cref="ColorRange"/>.
        /// </summary>
        [Parameter]
        public string? Fill { get; set; }

        /// <summary>
        /// The cell border color.
        /// </summary>
        [Parameter]
        public string? Stroke { get; set; }

        /// <summary>
        /// The cell border width in pixels.
        /// </summary>
        [Parameter]
        public double StrokeWidth { get; set; }

        /// <summary>
        /// The value-to-color mapping for cells. Each range specifies a <c>Min</c>, <c>Max</c> and <c>Color</c>.
        /// Cells with an intensity falling inside a range are filled with its color.
        /// </summary>
        [Parameter]
        public IList<SeriesColorRange>? ColorRange { get; set; }

        /// <summary>
        /// The label used for the intensity dimension in the tooltip. Defaults to <c>"Value"</c>.
        /// </summary>
        [Parameter]
        public string IntensityLabel { get; set; } = "Value";

        /// <inheritdoc />
        public override string Color => Fill ?? ColorRange?.FirstOrDefault()?.Color ?? Stroke ?? string.Empty;

        internal Func<TItem, double> Intensity
        {
            get
            {
                if (string.IsNullOrEmpty(IntensityProperty))
                {
                    return _ => 0;
                }

                return PropertyAccess.Getter<TItem, double>(IntensityProperty);
            }
        }

        internal double EffectiveCellWidth(ScaleBase categoryScale)
        {
            if (CellWidth > 0)
            {
                return CellWidth;
            }

            var getter = Category(categoryScale);
            return InferStep(Items.Select(getter));
        }

        internal double EffectiveCellHeight()
        {
            if (CellHeight > 0)
            {
                return CellHeight;
            }

            return InferStep(Items.Select(Value));
        }

        private static double InferStep(IEnumerable<double> values)
        {
            var distinct = values.Distinct().OrderBy(v => v).ToList();
            if (distinct.Count < 2)
            {
                return 1;
            }

            double step = double.MaxValue;
            for (var i = 1; i < distinct.Count; i++)
            {
                var gap = distinct[i] - distinct[i - 1];
                if (gap > 0 && gap < step)
                {
                    step = gap;
                }
            }

            return step == double.MaxValue ? 1 : step;
        }

        /// <inheritdoc />
        public override ScaleBase TransformCategoryScale(ScaleBase scale)
        {
            var result = base.TransformCategoryScale(scale);
            ExpandScaleByHalfCell(result, EffectiveCellWidth(result) / 2);
            return result;
        }

        /// <inheritdoc />
        public override ScaleBase TransformValueScale(ScaleBase scale)
        {
            var result = base.TransformValueScale(scale);
            ExpandScaleByHalfCell(result, EffectiveCellHeight() / 2);
            return result;
        }

        private static void ExpandScaleByHalfCell(ScaleBase scale, double halfCell)
        {
            if (halfCell <= 0 || scale.Input == null)
            {
                return;
            }

            scale.Input.MergeWidth(new ScaleRange
            {
                Start = scale.Input.Start - halfCell,
                End = scale.Input.End + halfCell,
            });
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
            var value = ComposeValue(chart.GetValueScale(ValueAxisName));
            var halfWidth = chart.CategoryScale.Scale(EffectiveCellWidth(chart.CategoryScale), true) - chart.CategoryScale.Scale(0, true);
            var halfHeight = chart.GetValueScale(ValueAxisName).Scale(0, true) - chart.GetValueScale(ValueAxisName).Scale(EffectiveCellHeight(), true);
            halfWidth = Math.Abs(halfWidth) / 2;
            halfHeight = Math.Abs(halfHeight) / 2;

            return Items.Any(item =>
            {
                var cx = category(item);
                var cy = value(item);
                return x >= cx - halfWidth && x <= cx + halfWidth &&
                       y >= cy - halfHeight && y <= cy + halfHeight;
            });
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
            var value = ComposeValue(chart.GetValueScale(ValueAxisName));

            var result = Items.Select(item =>
            {
                var cx = category(item);
                var cy = value(item);
                var dx = cx - x;
                var dy = cy - y;
                return new { Item = item, Distance = Math.Sqrt(dx * dx + dy * dy), Point = new Point { X = cx, Y = cy } };
            }).Aggregate((a, b) => a.Distance < b.Distance ? a : b);

            return (result.Item!, result.Point);
        }

        /// <inheritdoc />
        protected override RenderFragment RenderLegendItem(bool clickable)
        {
            if (ColorRange == null || ColorRange.Count == 0)
            {
                return base.RenderLegendItem(clickable);
            }

            var chart = RequireChart();
            var index = chart.Series.IndexOf(this);

            return builder =>
            {
                var seq = 0;
                for (var r = 0; r < ColorRange.Count; r++)
                {
                    var range = ColorRange[r];
                    builder.OpenComponent<Rendering.LegendItem>(seq++);
                    builder.AddAttribute(seq++, nameof(Rendering.LegendItem.Index), index);
                    builder.AddAttribute(seq++, nameof(Rendering.LegendItem.Color), range.Color);
                    builder.AddAttribute(seq++, nameof(Rendering.LegendItem.MarkerType), MarkerType.Square);
                    builder.AddAttribute(seq++, nameof(Rendering.LegendItem.MarkerSize), MarkerSize);
                    builder.AddAttribute(seq++, nameof(Rendering.LegendItem.Text), FormatRangeLabel(range));
                    builder.AddAttribute(seq++, nameof(Rendering.LegendItem.Clickable), false);
                    builder.CloseComponent();
                }
            };
        }

        private static string FormatRangeLabel(SeriesColorRange range)
        {
            var min = double.IsNegativeInfinity(range.Min) ? "-∞" : range.Min.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            var max = double.IsPositiveInfinity(range.Max) ? "+∞" : range.Max.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
            return $"{min} – {max}";
        }

        /// <inheritdoc />
        protected override string TooltipTitle(TItem item)
        {
            var chart = RequireChart();
            var category = Category(chart.CategoryScale);
            var xText = chart.CategoryAxis.Format(chart.CategoryScale, chart.CategoryScale.Value(category(item)));
            var vs = chart.GetValueScale(ValueAxisName);
            var yText = chart.GetValueAxis(ValueAxisName).Format(vs, vs.Value(Value(item)));
            return $"{xText}, {yText}";
        }

        /// <inheritdoc />
        protected override string TooltipValue(TItem item)
        {
            if (string.IsNullOrEmpty(IntensityProperty))
            {
                return base.TooltipValue(item);
            }

            var intensity = Intensity(item);
            return $"{IntensityLabel}: {intensity.ToInvariantString()}";
        }
    }
}
