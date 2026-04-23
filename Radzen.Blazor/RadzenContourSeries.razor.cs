using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// A chart series that renders a filled contour (isoband) plot from scalar field data sampled on a
    /// regular grid. Each data item provides an X coordinate
    /// (<see cref="CartesianSeries{TItem}.CategoryProperty"/>), a Y coordinate
    /// (<see cref="CartesianSeries{TItem}.ValueProperty"/>) and an intensity value
    /// (<see cref="IntensityProperty"/>). The series draws one filled polygon per
    /// (cell, band) so that areas with intensity within a given <see cref="SeriesColorRange"/> are
    /// shaded with that range's color.
    /// <para>
    /// Uses linear interpolation inside each grid triangle (marching-triangles), which avoids the saddle
    /// ambiguity of marching-squares and produces smooth iso-bands suitable for isoilluminance plots,
    /// temperature maps and similar visualisations.
    /// </para>
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenChart&gt;
    ///     &lt;RadzenContourSeries Data=@grid CategoryProperty="X" ValueProperty="Y" IntensityProperty="Lux"
    ///         ColorRange="@ranges" ShowLines="true" Title="Illuminance" /&gt;
    ///     &lt;RadzenCategoryAxis Min="0" Max="5" /&gt;
    ///     &lt;RadzenValueAxis Min="0" Max="5" /&gt;
    /// &lt;/RadzenChart&gt;
    /// </code>
    /// </example>
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2026, Justification = TrimMessages.DataTypePreserved)]
    public partial class RadzenContourSeries<TItem> : CartesianSeries<TItem>
    {
        /// <summary>
        /// The name of the numeric property of <typeparamref name="TItem"/> that provides the scalar field
        /// (the value mapped to color).
        /// </summary>
        [Parameter]
        public string? IntensityProperty { get; set; }

        /// <summary>
        /// The default fill color applied to regions whose intensity does not match any entry in
        /// <see cref="ColorRange"/>.
        /// </summary>
        [Parameter]
        public string? Fill { get; set; }

        /// <summary>
        /// The color used for iso-lines when <see cref="ShowLines"/> is <c>true</c>. When not set the
        /// band color is used.
        /// </summary>
        [Parameter]
        public string? LineColor { get; set; }

        /// <summary>
        /// The iso-line width in pixels.
        /// </summary>
        [Parameter]
        public double LineWidth { get; set; } = 1;

        /// <summary>
        /// When <c>true</c> iso-lines are drawn between bands. Defaults to <c>false</c>.
        /// </summary>
        [Parameter]
        public bool ShowLines { get; set; }

        /// <summary>
        /// Explicit iso-line thresholds. When <c>null</c> the <c>Min</c> of each entry in
        /// <see cref="ColorRange"/> is used.
        /// </summary>
        [Parameter]
        public IList<double>? LineThresholds { get; set; }

        /// <summary>
        /// The value-to-color mapping for isoband fills. Each range specifies a <c>Min</c>, <c>Max</c>
        /// and <c>Color</c>.
        /// </summary>
        [Parameter]
        public IList<SeriesColorRange>? ColorRange { get; set; }

        /// <summary>
        /// The label used for the intensity dimension in the tooltip. Defaults to <c>"Value"</c>.
        /// </summary>
        [Parameter]
        public string IntensityLabel { get; set; } = "Value";

        /// <inheritdoc />
        public override string Color => Fill ?? ColorRange?.FirstOrDefault()?.Color ?? string.Empty;

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

        /// <summary>
        /// Builds the regular grid lookup from <see cref="CartesianSeries{TItem}.Items"/>. Returns the
        /// sorted unique X and Y values and a 2D array of intensities indexed as <c>[xIndex, yIndex]</c>.
        /// Cells with missing corner values are represented as <see cref="double.NaN"/>.
        /// </summary>
        internal (double[] xs, double[] ys, double[,] grid) BuildGrid(ScaleBase categoryScale)
        {
            var categoryGetter = Category(categoryScale);
            var pairs = Items.Select(item => (X: categoryGetter(item), Y: Value(item), V: Intensity(item))).ToList();

            var xs = pairs.Select(p => p.X).Distinct().OrderBy(v => v).ToArray();
            var ys = pairs.Select(p => p.Y).Distinct().OrderBy(v => v).ToArray();

            var xIndex = xs.Select((x, i) => (x, i)).ToDictionary(p => p.x, p => p.i);
            var yIndex = ys.Select((y, i) => (y, i)).ToDictionary(p => p.y, p => p.i);

            var grid = new double[xs.Length, ys.Length];
            for (var i = 0; i < xs.Length; i++)
            {
                for (var j = 0; j < ys.Length; j++)
                {
                    grid[i, j] = double.NaN;
                }
            }

            foreach (var p in pairs)
            {
                grid[xIndex[p.X], yIndex[p.Y]] = p.V;
            }

            return (xs, ys, grid);
        }

        /// <inheritdoc />
        public override ScaleBase TransformValueScale(ScaleBase scale)
        {
            // Extend the value axis so that the full grid is included even when axis Min/Max are not set.
            return base.TransformValueScale(scale);
        }

        internal readonly struct Vertex
        {
            public Vertex(double x, double y, double v) { X = x; Y = y; V = v; }
            public double X { get; }
            public double Y { get; }
            public double V { get; }
        }

        internal static List<Vertex> ClipLow(List<Vertex> polygon, double threshold)
        {
            var result = new List<Vertex>();
            if (polygon.Count == 0)
            {
                return result;
            }

            for (var i = 0; i < polygon.Count; i++)
            {
                var curr = polygon[i];
                var prev = polygon[(i - 1 + polygon.Count) % polygon.Count];
                var currIn = curr.V >= threshold;
                var prevIn = prev.V >= threshold;

                if (currIn != prevIn)
                {
                    var denom = curr.V - prev.V;
                    var t = denom == 0 ? 0 : (threshold - prev.V) / denom;
                    result.Add(new Vertex(
                        prev.X + t * (curr.X - prev.X),
                        prev.Y + t * (curr.Y - prev.Y),
                        threshold));
                }

                if (currIn)
                {
                    result.Add(curr);
                }
            }

            return result;
        }

        internal static List<Vertex> ClipHigh(List<Vertex> polygon, double threshold)
        {
            var result = new List<Vertex>();
            if (polygon.Count == 0)
            {
                return result;
            }

            for (var i = 0; i < polygon.Count; i++)
            {
                var curr = polygon[i];
                var prev = polygon[(i - 1 + polygon.Count) % polygon.Count];
                var currIn = curr.V <= threshold;
                var prevIn = prev.V <= threshold;

                if (currIn != prevIn)
                {
                    var denom = curr.V - prev.V;
                    var t = denom == 0 ? 0 : (threshold - prev.V) / denom;
                    result.Add(new Vertex(
                        prev.X + t * (curr.X - prev.X),
                        prev.Y + t * (curr.Y - prev.Y),
                        threshold));
                }

                if (currIn)
                {
                    result.Add(curr);
                }
            }

            return result;
        }

        private static (Vertex A, Vertex B)? TriangleIsoSegment(Vertex a, Vertex b, Vertex c, double level)
        {
            Vertex? first = null;
            Vertex? second = null;

            void CheckEdge(Vertex p, Vertex q)
            {
                var pAbove = p.V > level;
                var qAbove = q.V > level;
                if (pAbove == qAbove)
                {
                    return;
                }

                var denom = q.V - p.V;
                if (denom == 0)
                {
                    return;
                }

                var t = (level - p.V) / denom;
                var v = new Vertex(p.X + t * (q.X - p.X), p.Y + t * (q.Y - p.Y), level);
                if (first == null)
                {
                    first = v;
                }
                else if (second == null)
                {
                    second = v;
                }
            }

            CheckEdge(a, b);
            CheckEdge(b, c);
            CheckEdge(c, a);

            if (first.HasValue && second.HasValue)
            {
                return (first.Value, second.Value);
            }

            return null;
        }

        internal static string BuildPolygonPoints(List<Vertex> polygon)
        {
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < polygon.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(' ');
                }
                sb.Append(polygon[i].X.ToString("R", CultureInfo.InvariantCulture));
                sb.Append(',');
                sb.Append(polygon[i].Y.ToString("R", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        internal void RenderBands(RenderTreeBuilder builder, ref int sequence, ScaleBase categoryScale, ScaleBase valueScale)
        {
            if (ColorRange == null || ColorRange.Count == 0 || !Items.Any())
            {
                return;
            }

            var (xs, ys, grid) = BuildGrid(categoryScale);
            if (xs.Length < 2 || ys.Length < 2)
            {
                return;
            }

            var xPx = xs.Select(x => categoryScale.Scale(x, true)).ToArray();
            var yPx = ys.Select(y => valueScale.Scale(y, true)).ToArray();

            foreach (var range in ColorRange)
            {
                for (var i = 0; i < xs.Length - 1; i++)
                {
                    for (var j = 0; j < ys.Length - 1; j++)
                    {
                        var v00 = grid[i, j];
                        var v10 = grid[i + 1, j];
                        var v01 = grid[i, j + 1];
                        var v11 = grid[i + 1, j + 1];
                        if (double.IsNaN(v00) || double.IsNaN(v10) || double.IsNaN(v01) || double.IsNaN(v11))
                        {
                            continue;
                        }

                        var p00 = new Vertex(xPx[i], yPx[j], v00);
                        var p10 = new Vertex(xPx[i + 1], yPx[j], v10);
                        var p01 = new Vertex(xPx[i], yPx[j + 1], v01);
                        var p11 = new Vertex(xPx[i + 1], yPx[j + 1], v11);

                        EmitClippedTriangle(builder, ref sequence, p00, p10, p11, range);
                        EmitClippedTriangle(builder, ref sequence, p00, p11, p01, range);
                    }
                }
            }
        }

        private static void EmitClippedTriangle(RenderTreeBuilder builder, ref int sequence, Vertex a, Vertex b, Vertex c, SeriesColorRange range)
        {
            var poly = new List<Vertex> { a, b, c };
            poly = ClipLow(poly, range.Min);
            if (poly.Count < 3)
            {
                return;
            }
            poly = ClipHigh(poly, range.Max);
            if (poly.Count < 3)
            {
                return;
            }

            var points = BuildPolygonPoints(poly);
            builder.OpenElement(sequence++, "polygon");
            builder.AddAttribute(sequence++, "points", points);
            builder.AddAttribute(sequence++, "fill", range.Color);
            builder.AddAttribute(sequence++, "stroke", "none");
            builder.CloseElement();
        }

        internal void RenderIsoLines(RenderTreeBuilder builder, ref int sequence, ScaleBase categoryScale, ScaleBase valueScale)
        {
            if (!ShowLines || !Items.Any())
            {
                return;
            }

            var thresholds = LineThresholds;
            if (thresholds == null || thresholds.Count == 0)
            {
                thresholds = ColorRange?.Select(r => r.Min).Where(m => !double.IsInfinity(m)).Distinct().OrderBy(v => v).ToList();
            }

            if (thresholds == null || thresholds.Count == 0)
            {
                return;
            }

            var (xs, ys, grid) = BuildGrid(categoryScale);
            if (xs.Length < 2 || ys.Length < 2)
            {
                return;
            }

            var xPx = xs.Select(x => categoryScale.Scale(x, true)).ToArray();
            var yPx = ys.Select(y => valueScale.Scale(y, true)).ToArray();

            foreach (var level in thresholds)
            {
                for (var i = 0; i < xs.Length - 1; i++)
                {
                    for (var j = 0; j < ys.Length - 1; j++)
                    {
                        var v00 = grid[i, j];
                        var v10 = grid[i + 1, j];
                        var v01 = grid[i, j + 1];
                        var v11 = grid[i + 1, j + 1];
                        if (double.IsNaN(v00) || double.IsNaN(v10) || double.IsNaN(v01) || double.IsNaN(v11))
                        {
                            continue;
                        }

                        var p00 = new Vertex(xPx[i], yPx[j], v00);
                        var p10 = new Vertex(xPx[i + 1], yPx[j], v10);
                        var p01 = new Vertex(xPx[i], yPx[j + 1], v01);
                        var p11 = new Vertex(xPx[i + 1], yPx[j + 1], v11);

                        EmitIsoLineForTriangle(builder, ref sequence, p00, p10, p11, level);
                        EmitIsoLineForTriangle(builder, ref sequence, p00, p11, p01, level);
                    }
                }
            }
        }

        private void EmitIsoLineForTriangle(RenderTreeBuilder builder, ref int sequence, Vertex a, Vertex b, Vertex c, double level)
        {
            var segment = TriangleIsoSegment(a, b, c, level);
            if (!segment.HasValue)
            {
                return;
            }

            var stroke = LineColor ?? PickColor(0, null, Fill, ColorRange, level) ?? "#000";
            builder.OpenElement(sequence++, "line");
            builder.AddAttribute(sequence++, "x1", segment.Value.A.X.ToString("R", CultureInfo.InvariantCulture));
            builder.AddAttribute(sequence++, "y1", segment.Value.A.Y.ToString("R", CultureInfo.InvariantCulture));
            builder.AddAttribute(sequence++, "x2", segment.Value.B.X.ToString("R", CultureInfo.InvariantCulture));
            builder.AddAttribute(sequence++, "y2", segment.Value.B.Y.ToString("R", CultureInfo.InvariantCulture));
            builder.AddAttribute(sequence++, "stroke", stroke);
            builder.AddAttribute(sequence++, "stroke-width", LineWidth.ToInvariantString());
            builder.AddAttribute(sequence++, "fill", "none");
            builder.CloseElement();
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
            var min = double.IsNegativeInfinity(range.Min) ? "-∞" : range.Min.ToString("R", CultureInfo.InvariantCulture);
            var max = double.IsPositiveInfinity(range.Max) ? "+∞" : range.Max.ToString("R", CultureInfo.InvariantCulture);
            return $"{min} – {max}";
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

            return Items.Any(item =>
            {
                var dx = category(item) - x;
                var dy = value(item) - y;
                return Math.Sqrt(dx * dx + dy * dy) <= tolerance;
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
                var px = category(item);
                var py = value(item);
                var dx = px - x;
                var dy = py - y;
                return new { Item = item, Distance = Math.Sqrt(dx * dx + dy * dy), Point = new Point { X = px, Y = py } };
            }).Aggregate((a, b) => a.Distance < b.Distance ? a : b);

            return (result.Item!, result.Point);
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
    }
}
