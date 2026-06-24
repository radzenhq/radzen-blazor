using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A chart series that displays data as a circular pie chart with segments representing proportions of a whole.
    /// RadzenPieSeries is ideal for showing percentage breakdowns, composition analysis, or relative comparisons of parts to a total.
    /// Divides a circle into segments where each segment's angle is proportional to its value relative to the sum of all values.
    /// Supports segment color customization via Fills, borders via Strokes with custom radius and positioning, TotalAngle to create semi-circles or partial pie charts (e.g., gauge-like displays),
    /// StartAngle controlling where the first segment begins, optional labels showing values or percentages on segments, interactive tooltips showing category/value/percentage,
    /// and legend where each segment appears as a legend item using category values.
    /// Use CategoryProperty for segment labels (shown in legend/tooltip) and ValueProperty for the numeric value determining segment size. For a donut chart (pie with hollow center), use RadzenDonutSeries instead.
    /// </summary>
    /// <typeparam name="TItem">The type of data items in the series. Each item represents one pie segment.</typeparam>
    /// <example>
    /// Basic pie chart:
    /// <code>
    /// &lt;RadzenChart&gt;
    ///     &lt;RadzenPieSeries Data=@marketShare CategoryProperty="Company" ValueProperty="Share" /&gt;
    /// &lt;/RadzenChart&gt;
    /// </code>
    /// Pie with custom colors and data labels:
    /// <code>
    /// &lt;RadzenChart&gt;
    ///     &lt;RadzenPieSeries Data=@data CategoryProperty="Category" ValueProperty="Value" 
    ///                      Fills=@(new[] { "#FF6384", "#36A2EB", "#FFCE56", "#4BC0C0" })&gt;
    ///         &lt;RadzenSeriesDataLabels Visible="true" /&gt;
    ///     &lt;/RadzenPieSeries&gt;
    /// &lt;/RadzenChart&gt;
    /// </code>
    /// </example>
    [UnconditionalSuppressMessage(TrimMessages.Trimming, TrimMessages.IL2026, Justification = TrimMessages.DataTypePreserved)]
    public partial class RadzenPieSeries<TItem> : CartesianSeries<TItem>
    {
        /// <summary>
        /// Gets or sets the horizontal center position of the pie chart in pixels.
        /// If not set, the pie is automatically centered horizontally within the chart area.
        /// </summary>
        /// <value>The X coordinate in pixels, or null for automatic horizontal centering.</value>
        [Parameter]
        public double? X { get; set; }

        /// <summary>
        /// Gets or sets the vertical center position of the pie chart in pixels.
        /// If not set, the pie is automatically centered vertically within the chart area.
        /// </summary>
        /// <value>The Y coordinate in pixels, or null for automatic vertical centering.</value>
        [Parameter]
        public double? Y { get; set; }

        /// <summary>
        /// Gets or sets the radius of the pie chart in pixels.
        /// If not set, the radius is automatically calculated to fit the available chart space.
        /// </summary>
        /// <value>The radius in pixels, or null for automatic sizing.</value>
        [Parameter]
        public double? Radius { get; set; }

        /// <summary>
        /// Gets or sets the corner radius in pixels used to round the corners of each segment.
        /// A pie segment has its two outer corners and its center apex rounded; a donut segment has all four corners rounded.
        /// The value is clamped per segment so that adjacent corners never overlap.
        /// </summary>
        /// <value>The corner radius in pixels. Default is <c>0</c> (sharp corners).</value>
        [Parameter]
        public double CornerRadius { get; set; }

        /// <summary>
        /// Gets or sets the width in pixels of a uniform gap drawn between adjacent segments. Each segment's
        /// two radial edges are inset by half this value, so neighboring segments are separated by a
        /// constant-width space from the rim to the center (or inner hole), independent of segment size.
        /// Composes with <see cref="CornerRadius"/>. A segment too narrow for the gap is skipped.
        /// </summary>
        /// <value>The gap width in pixels. Default is <c>0</c> (no gap).</value>
        [Parameter]
        public double SegmentGap { get; set; }

        /// <summary>
        /// Gets or sets the name of the property that provides a per-item radius value.
        /// When set, each pie/donut segment can have a different outer radius, allowing visual differentiation by size.
        /// The property should return a numeric value that is used to scale the radius relative to the maximum value.
        /// </summary>
        /// <value>The property name on <typeparamref name="TItem"/> that provides the radius value, or null to use a uniform radius.</value>
        [Parameter]
        public string? RadiusProperty { get; set; }

        /// <summary>
        /// Gets or sets the distance in pixels that a segment moves outward from the center when hovered.
        /// Set to a value greater than 0 to enable the explode-on-hover effect. Also sets the distance for
        /// segments pulled out permanently via <see cref="ExplodedProperty"/>.
        /// </summary>
        /// <value>The explode offset in pixels. Default is 0 (disabled).</value>
        [Parameter]
        public double ExplodeOffset { get; set; }

        /// <summary>
        /// Gets or sets the name of a boolean property that marks segments as exploded (pulled out from the
        /// center) in their normal state, not just on hover. Requires <see cref="ExplodeOffset"/> greater than 0,
        /// which sets the distance. Exploded segments stay pulled out and move a little further on hover; other
        /// segments still explode on hover.
        /// </summary>
        /// <value>The boolean property name on <typeparamref name="TItem"/>, or null to disable static explode.</value>
        [Parameter]
        public string? ExplodedProperty { get; set; }

        /// <summary>
        /// Gets or sets a collection of fill colors applied to individual pie segments in sequence.
        /// Each segment gets the color at its index position. If fewer colors than segments, colors are reused cyclically.
        /// If not set, uses the chart's color scheme.
        /// </summary>
        /// <value>An enumerable collection of CSS color values for segment backgrounds.</value>
        [Parameter]
        public IEnumerable<string>? Fills { get; set; }

        /// <summary>
        /// Gets or sets a collection of stroke (border) colors applied to individual pie segments in sequence.
        /// Use with <see cref="StrokeWidth"/> to create visible segment borders.
        /// </summary>
        /// <value>An enumerable collection of CSS color values for segment borders.</value>
        [Parameter]
        public IEnumerable<string>? Strokes { get; set; }

        /// <summary>
        /// Gets or sets the width of the pie segment borders in pixels.
        /// Set to 0 for no borders, or increase to make segment divisions more visible.
        /// </summary>
        /// <value>The stroke width in pixels. Default is 0 (no borders).</value>
        [Parameter]
        public double StrokeWidth { get; set; }

        /// <summary>
        /// Specifies how the segments are filled. Set to <see cref="FillMode.Solid"/> by default.
        /// Use <see cref="FillMode.Gradient"/> for a radial fill that holds the full segment color through the inner part of the radius and eases to a faded outer area.
        /// The faded part lets the chart background show through, so it is lighter on light themes and darker on dark themes. Use <see cref="FillMode.None"/> to render only the outline.
        /// </summary>
        /// <value>The fill mode. Default is <see cref="FillMode.Solid"/>.</value>
        [Parameter]
        public FillMode FillMode { get; set; } = FillMode.Solid;

        /// <summary>
        /// Specifies the opacity of the faded middle stop of the gradient.
        /// Below <c>1</c> it lets the chart background show through, so it eases lighter on light themes and darker on dark themes. Used when <see cref="FillMode"/> is <see cref="FillMode.Gradient"/>.
        /// </summary>
        /// <value>The gradient start opacity. Default is <c>0.62</c>.</value>
        [Parameter]
        public double GradientStartOpacity { get; set; } = 0.62;

        /// <summary>
        /// Specifies the opacity of the full-color part of the gradient - the center of a pie, or the outer rim of a donut ring. Used when <see cref="FillMode"/> is <see cref="FillMode.Gradient"/>.
        /// </summary>
        /// <value>The gradient end opacity. Default is <c>1</c>.</value>
        [Parameter]
        public double GradientEndOpacity { get; set; } = 1.0;

        /// <summary>
        /// Specifies the radius fraction (0-1) at which the gradient holds the full color before it starts to fade. Used when <see cref="FillMode"/> is <see cref="FillMode.Gradient"/>.
        /// </summary>
        /// <value>The gradient inner offset. Default is <c>0.4</c>.</value>
        [Parameter]
        public double GradientInnerOffset { get; set; } = 0.4;

        /// <summary>
        /// Specifies the radius fraction (0-1) of the gradient's faded middle stop. Used when <see cref="FillMode"/> is <see cref="FillMode.Gradient"/>.
        /// </summary>
        /// <value>The gradient middle offset. Default is <c>0.9</c>.</value>
        [Parameter]
        public double GradientMidOffset { get; set; } = 0.9;

        /// <summary>
        /// Specifies the opacity at the very rim of the gradient. Used when <see cref="FillMode"/> is <see cref="FillMode.Gradient"/>.
        /// </summary>
        /// <value>The gradient rim opacity. Default is <c>0.7</c>.</value>
        [Parameter]
        public double GradientRimOpacity { get; set; } = 0.7;

        /// <summary>
        /// Gets or sets a value indicating whether hovering or clicking a legend item displays the tooltip for the corresponding pie/donut segment.
        /// This is useful when small slices are difficult to hover over directly on the chart.
        /// </summary>
        /// <value><c>true</c> to show tooltips on legend item interaction; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool ShowTooltipOnLegend { get; set; } = true;

        /// <summary>
        /// Gets or sets the starting angle (in degrees) from which pie segments begin rendering, measured clockwise from the right (0°).
        /// Use to rotate the pie: 90° (top), 0° (right), 180° (left), 270° (bottom).
        /// </summary>
        /// <value>The start angle in degrees. Default is 90 (top of circle).</value>
        [Parameter]
        public double StartAngle { get; set; } = 90;

        /// <summary>
        /// Gets or sets the total angle span of the pie in degrees.
        /// Use 360 for a full circle, 180 for a semi-circle, or other values for partial pies (useful for gauge-like visualizations).
        /// </summary>
        /// <value>The total angle in degrees. Default is 360 (full circle).</value>
        [Parameter]
        public double TotalAngle { get; set; } = 360;

        /// <summary>
        /// Returns the current radius - either a specified <see cref="Radius" /> or automatically calculated one.
        /// </summary>
        protected double CurrentRadius
        {
            get
            {
                if (Chart?.CategoryScale?.Output != null && Chart?.ValueScale?.Output != null)
                {
                    return Radius ?? Math.Min(Chart.CategoryScale.Output.Mid, Chart.ValueScale.Output.Mid);
                }
                return Radius ?? 0;
            }
        }

        /// <summary>
        /// Returns the radius for a specific data item. If <see cref="RadiusProperty"/> is set,
        /// the radius is scaled proportionally between a minimum (65% of max) and the max radius.
        /// </summary>
        internal double GetItemRadius(TItem item, double maxRadius)
        {
            if (string.IsNullOrEmpty(RadiusProperty))
            {
                return maxRadius;
            }

            var getter = PropertyAccess.Getter<TItem, double>(RadiusProperty);
            var items = PositiveItems;

            if (!items.Any())
            {
                return maxRadius;
            }

            var maxValue = items.Max(getter);
            var minValue = items.Min(getter);

            if (maxValue == minValue)
            {
                return maxRadius;
            }

            var value = getter(item);
            var minRadius = maxRadius * 0.65;

            return minRadius + (value - minValue) / (maxValue - minValue) * (maxRadius - minRadius);
        }

        /// <summary>
        /// Returns whether a specific data item is exploded in its normal state, as determined by
        /// <see cref="ExplodedProperty"/>. Always <c>false</c> when <see cref="ExplodedProperty"/> is not set.
        /// </summary>
        internal bool IsExploded(TItem item)
        {
            if (string.IsNullOrEmpty(ExplodedProperty))
            {
                return false;
            }

            return PropertyAccess.Getter<TItem, bool>(ExplodedProperty)(item);
        }

        /// <summary>
        /// Gets the current X coordinate of the center.
        /// </summary>
        internal double CenterX
        {
            get
            {
                if (Chart?.CategoryScale?.Output != null)
                {
                    return X ?? Chart.CategoryScale.Output.Mid + 8;
                }
                return X ?? 0;
            }
        }

        /// <summary>
        /// Gets the current Y coordinate of the center.
        /// </summary>
        internal double CenterY
        {
            get
            {
                if (Chart?.ValueScale?.Output != null)
                {
                    return Y ?? Chart.ValueScale.Output.Mid;
                }
                return Y ?? 0;
            }
        }
        /// <inheritdoc />
        public override string Color
        {
            get
            {
                return "#000";
            }
        }

        /// <summary>
        /// Stores Data filtered to items greater than zero as an IList of <typeparamref name="TItem"/>.
        /// </summary>
        /// <value>The items.</value>
        protected IList<TItem> PositiveItems => Items != null ? Items.Where(e => Value(e) > 0).ToList() : new List<TItem>();

        /// <inheritdoc />
        public override double MeasureLegend()
        {
            if (Items.Any())
            {
                return Items.Select(item => TextMeasurer.TextWidth(TooltipTitle(item))).Max() + MarkerSize;
            }

            return 0;
        }

        /// <inheritdoc />
        public override IEnumerable<double> MeasureLegendItems()
        {
            return Items.Select(item => TextMeasurer.TextWidth(TooltipTitle(item)));
        }

        /// <inheritdoc />
        protected override RenderFragment RenderLegendItem(bool clickable)
        {
            return builder =>
            {
                foreach (var data in Items)
                {
                    builder.OpenComponent<LegendItem>(0);
                    builder.AddAttribute(1, nameof(LegendItem.Text), TooltipTitle(data));
                    builder.AddAttribute(2, nameof(LegendItem.Class), $"rz-series-item-{Items.IndexOf(data)}");
                    builder.AddAttribute(3, nameof(LegendItem.MarkerSize), MarkerSize);
                    builder.AddAttribute(4, nameof(LegendItem.MarkerType), MarkerType);
                    builder.AddAttribute(5, nameof(LegendItem.Color), PickColor(Items.IndexOf(data), Fills));
                    builder.AddAttribute(6, nameof(LegendItem.Click), EventCallback.Factory.Create(this, () => OnLegendClick(data!)));
                    builder.AddAttribute(7, nameof(LegendItem.Clickable), clickable);

                    if (ShowTooltipOnLegend)
                    {
                        builder.AddAttribute(8, nameof(LegendItem.MouseEnter), EventCallback.Factory.Create<MouseEventArgs>(this, args => OnLegendMouseEnter(data!)));
                        builder.AddAttribute(9, nameof(LegendItem.MouseLeave), EventCallback.Factory.Create<MouseEventArgs>(this, args => OnLegendMouseLeave()));
                    }

                    builder.CloseComponent();
                }
                ;
            };
        }

        private object? hoveredLegendData;

        private async Task OnLegendMouseEnter(object data)
        {
            hoveredLegendData = data;

            var chart = RequireChart();
            if (chart != null)
            {
                await chart.ShowTooltip(this, data);
            }
        }

        private void OnLegendMouseLeave()
        {
            hoveredLegendData = null;
        }

        private async Task OnLegendClick(object data)
        {
            var chart = RequireChart();
            if (chart != null)
            {
                if (ShowTooltipOnLegend)
                {
                    await chart.ShowTooltip(this, data);
                }

                if (chart.LegendClick.HasDelegate)
                {
                    var args = new LegendClickEventArgs
                    {
                        Data = data,
                        Title = GetTitle(),
                        IsVisible = true,
                    };

                    await chart.LegendClick.InvokeAsync(args);
                }
            }
        }
        /// <inheritdoc />
        public override bool Contains(double x, double y, double tolerance)
        {
            if (Items.Any())
            {
                var distance = Math.Sqrt(Math.Pow(CenterX - x, 2) + Math.Pow(CenterY - y, 2));

                return distance <= CurrentRadius;
            }

            return false;
        }

        /// <summary>
        /// Returns the radius for a specific item for tooltip positioning.
        /// </summary>
        private double TooltipRadius(TItem item)
        {
            return GetItemRadius(item, CurrentRadius);
        }

        /// <inheritdoc />
        public override (object, Point) DataAt(double x, double y)
        {
            if (hoveredLegendData != null)
            {
                return (hoveredLegendData, new Point() { X = x, Y = y });
            }

            if (!Contains(x, y, 0))
            {
                return (default!, new Point());
            }

            var angle = Math.Atan2(CenterY - y, x - CenterX) * 180 / Math.PI;

            // Normalize the angle to be within [0, 360)
            angle = (angle + 360) % 360;

            var sum = Items.Sum(Value);
            var startAngle = StartAngle;

            foreach (var data in Items)
            {
                var value = Value(data);

                if (value == 0)
                {
                    continue;
                }

                var endAngle = startAngle - value / sum * TotalAngle; // assuming clockwise

                // Normalize the endAngle
                endAngle = (endAngle + 360) % 360;

                if ((startAngle >= endAngle && angle <= startAngle && angle >= endAngle) ||
                    (startAngle <= endAngle && (angle <= startAngle || angle >= endAngle)))
                {
                    return (data!, new Point() { X = x, Y = y });
                }

                startAngle = endAngle;
            }

            return (default!, new Point());
        }


        /// <inheritdoc />
        protected override string TooltipClass(TItem item)
        {
            return $"{base.TooltipClass(item)} rz-pie-tooltip rz-series-item-{Items.IndexOf(item)}";
        }

        /// <inheritdoc />
        protected override string TooltipStyle(TItem item)
        {
            var style = base.TooltipStyle(item);

            var index = Items.IndexOf(item);

            if (index >= 0)
            {
                var color = PickColor(index, Fills);

                if (color != null)
                {
                    style = $"{style}; border-color: {color};";
                }
            }

            return style;
        }

        private double TooltipAngle(TItem item)
        {
            var items = PositiveItems;
            var sum = items.Sum(Value);
            var startAngle = StartAngle;
            var endAngle = 0d;

            foreach (var data in items)
            {
                var value = Value(data);
                var sweepAngle = value / sum * TotalAngle;

                endAngle = startAngle - sweepAngle;

                if (EqualityComparer<TItem>.Default.Equals(data, item))
                {
                    break;
                }

                startAngle = endAngle;
            }

            return startAngle + (endAngle - startAngle) / 2;
        }

        /// <inheritdoc />
        internal override double TooltipX(TItem item)
        {
            var angle = TooltipAngle(item);

            return CenterX + TooltipRadius(item) * Math.Cos(DegToRad(angle));
        }

        /// <inheritdoc />
        internal override double TooltipY(TItem item)
        {
            var angle = TooltipAngle(item);

            return CenterY - TooltipRadius(item) * Math.Sin(DegToRad(angle));
        }

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        protected double DegToRad(double degrees)
        {
            return degrees * Math.PI / 180;
        }

        /// <summary>
        /// Converts radial coordinates to to cartesian.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="degrees">The degrees.</param>
        protected (double X, double Y) ToCartesian(double x, double y, double radius, double degrees)
        {
            var radians = DegToRad(degrees);

            return (x + radius * Math.Cos(radians), y - radius * Math.Sin(radians));
        }

        /// <summary>
        /// Creates SVG path that renders the specified segment, rounding its corners by <see cref="CornerRadius"/>.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="innerRadius">The inner radius.</param>
        /// <param name="startAngle">The start angle.</param>
        /// <param name="endAngle">The end angle.</param>
        protected string Segment(double x, double y, double radius, double innerRadius, double startAngle, double endAngle)
        {
            if (SegmentGap > 0.01)
            {
                return GappedSegment(x, y, radius, innerRadius, startAngle, endAngle, CornerRadius, SegmentGap);
            }

            return Segment(x, y, radius, innerRadius, startAngle, endAngle, CornerRadius);
        }

        /// <summary>
        /// Creates SVG path that renders the specified segment with sharp corners.
        /// </summary>
        private string SharpSegment(double x, double y, double radius, double innerRadius, double startAngle, double endAngle)
        {
            var largeArcFlag = 0;

            if (Math.Abs(endAngle - startAngle) >= 180)
            {
                endAngle += 0.01;
                largeArcFlag = 1;
            }

            var start = ToCartesian(x, y, radius, startAngle);
            var end = ToCartesian(x, y, radius, endAngle);

            var innerStart = ToCartesian(x, y, innerRadius, startAngle);
            var innerEnd = ToCartesian(x, y, innerRadius, endAngle);

            var startX = start.X.ToInvariantString();
            var startY = start.Y.ToInvariantString();
            var endX = end.X.ToInvariantString();
            var endY = end.Y.ToInvariantString();
            var r = radius.ToInvariantString();
            var innerStartX = innerStart.X.ToInvariantString();
            var innerStartY = innerStart.Y.ToInvariantString();
            var innerEndX = innerEnd.X.ToInvariantString();
            var innerEndY = innerEnd.Y.ToInvariantString();
            var innerR = innerRadius.ToInvariantString();

            return $"M {startX} {startY} A {r} {r} 0 {largeArcFlag} 1 {endX} {endY} L {innerEndX} {innerEndY} A {innerR} {innerR} 0 {largeArcFlag} 0 {innerStartX} {innerStartY} Z";
        }

        /// <summary>
        /// Creates SVG path that renders the specified segment with corners rounded by <paramref name="cornerRadius"/>.
        /// A pie segment (<paramref name="innerRadius"/> of <c>0</c>) rounds its two outer corners and its center apex;
        /// a donut segment rounds all four corners. The corner radius is clamped per segment so corners never overlap.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="innerRadius">The inner radius.</param>
        /// <param name="startAngle">The start angle.</param>
        /// <param name="endAngle">The end angle.</param>
        /// <param name="cornerRadius">The corner radius in pixels.</param>
        protected string Segment(double x, double y, double radius, double innerRadius, double startAngle, double endAngle, double cornerRadius)
        {
            var sweep = Math.Abs(startAngle - endAngle);

            // No rounding requested, a full ring (no corners to round), or a degenerate radius: use the sharp path.
            if (cornerRadius <= 0.01 || sweep >= 359.99 || radius <= 0)
            {
                return SharpSegment(x, y, radius, innerRadius, startAngle, endAngle);
            }

            var isDonut = innerRadius > 0;

            // Clamp the corner radius so adjacent fillets never overlap.
            var s = Math.Sin(DegToRad(sweep / 2));
            var rc = Math.Min(cornerRadius, (radius - innerRadius) / 2);
            rc = Math.Min(rc, radius * s / (1 + s));                                 // outer arc angular limit
            if (isDonut && s < 1)
            {
                rc = Math.Min(rc, innerRadius * s / (1 - s));                        // inner arc angular limit
            }

            // Too thin to round visibly - fall back to sharp corners.
            if (rc < 0.01)
            {
                return SharpSegment(x, y, radius, innerRadius, startAngle, endAngle);
            }

            // The slice spans from startAngle (a0) down to endAngle (a1), drawn clockwise.
            var a0 = startAngle;
            var a1 = endAngle;

            // Outer fillets are tangent to the outer circle from inside (center at radius - rc).
            var eOut = Math.Asin(rc / (radius - rc));
            var eOutDeg = eOut * 180 / Math.PI;
            var outerEdgeRadius = (radius - rc) * Math.Cos(eOut);

            var outerArcStart = ToCartesian(x, y, radius, a0 - eOutDeg);
            var outerArcEnd = ToCartesian(x, y, radius, a1 + eOutDeg);
            var outerEdgeStart = ToCartesian(x, y, outerEdgeRadius, a0);
            var outerEdgeEnd = ToCartesian(x, y, outerEdgeRadius, a1);
            var outerCenterStart = ToCartesian(x, y, radius - rc, a0 - eOutDeg);
            var outerCenterEnd = ToCartesian(x, y, radius - rc, a1 + eOutDeg);

            var largeOuter = (sweep - 2 * eOutDeg) >= 180 ? 1 : 0;

            var path = $"M {Format(outerArcStart)}" +
                       $" {Arc(radius, largeOuter, 1, outerArcEnd)}" +
                       $" {Arc(rc, 0, Sweep(outerArcEnd, outerEdgeEnd, outerCenterEnd), outerEdgeEnd)}";

            if (isDonut)
            {
                // Inner fillets are tangent to the inner circle from outside (center at innerRadius + rc).
                var eIn = Math.Asin(rc / (innerRadius + rc));
                var eInDeg = eIn * 180 / Math.PI;
                var innerEdgeRadius = (innerRadius + rc) * Math.Cos(eIn);

                var innerArcStart = ToCartesian(x, y, innerRadius, a0 - eInDeg);
                var innerArcEnd = ToCartesian(x, y, innerRadius, a1 + eInDeg);
                var innerEdgeStart = ToCartesian(x, y, innerEdgeRadius, a0);
                var innerEdgeEnd = ToCartesian(x, y, innerEdgeRadius, a1);
                var innerCenterStart = ToCartesian(x, y, innerRadius + rc, a0 - eInDeg);
                var innerCenterEnd = ToCartesian(x, y, innerRadius + rc, a1 + eInDeg);

                var largeInner = (sweep - 2 * eInDeg) >= 180 ? 1 : 0;

                path += $" L {Format(innerEdgeEnd)}" +
                        $" {Arc(rc, 0, Sweep(innerEdgeEnd, innerArcEnd, innerCenterEnd), innerArcEnd)}" +
                        $" {Arc(innerRadius, largeInner, 0, innerArcStart)}" +
                        $" {Arc(rc, 0, Sweep(innerArcStart, innerEdgeStart, innerCenterStart), innerEdgeStart)}" +
                        $" L {Format(outerEdgeStart)}" +
                        $" {Arc(rc, 0, Sweep(outerEdgeStart, outerArcStart, outerCenterStart), outerArcStart)} Z";
            }
            else
            {
                // Pie: round only the outer corners and keep the center apex sharp, so every slice meets the
                // exact center regardless of size (rounding the apex would push thin slices away from it).
                path += $" L {Format((x, y))}" +
                        $" L {Format(outerEdgeStart)}" +
                        $" {Arc(rc, 0, Sweep(outerEdgeStart, outerArcStart, outerCenterStart), outerArcStart)} Z";
            }

            return path;
        }

        /// <summary>
        /// Creates an SVG path for a segment separated from its neighbors by a uniform <paramref name="gap"/>.
        /// Each radial edge is inset by <c>gap / 2</c> perpendicular to the radius, so the space between
        /// adjacent segments has a constant width. Composes with <paramref name="cornerRadius"/>; segments too
        /// narrow for the gap return an empty path and are not rendered.
        /// </summary>
        private string GappedSegment(double x, double y, double radius, double innerRadius, double startAngle, double endAngle, double cornerRadius, double gap)
        {
            var sweep = Math.Abs(startAngle - endAngle);

            // A full ring has no radial edges to inset.
            if (sweep >= 359.99 || radius <= 0)
            {
                return SharpSegment(x, y, radius, innerRadius, startAngle, endAngle);
            }

            var isDonut = innerRadius > 0;
            var h = gap / 2;
            var halfRad = DegToRad(sweep / 2);
            var s = Math.Sin(halfRad);

            // The segment is narrower than the gap: its inset edges cross before reaching the rim.
            if (h >= radius * s - 1e-6)
            {
                return "";
            }

            // The hole is smaller than the gap, or the inner edges would cross.
            if (isDonut && (h >= innerRadius - 1e-6 || Math.Asin(Math.Min(1, h / innerRadius)) >= halfRad - 1e-9))
            {
                return "";
            }

            var a0 = startAngle;
            var a1 = endAngle;
            var am = (a0 + a1) / 2;

            var nStart = InteriorNormal(a0, am);
            var nEnd = InteriorNormal(a1, am);

            var rc = cornerRadius <= 0.01 ? 0 : Math.Min(cornerRadius, (radius - innerRadius) / 2);
            if (rc > 0)
            {
                rc = Math.Min(rc, (radius * s - h) / (1 + s));
                if (isDonut && s < 1)
                {
                    rc = Math.Min(rc, (innerRadius * s - h) / (1 - s));
                }
            }

            if (rc < 0.01)
            {
                // Gap only, sharp corners.
                var dOut = Math.Asin(Math.Min(1, h / radius)) * 180 / Math.PI;
                var oStart = ToCartesian(x, y, radius, a0 - dOut);
                var oEnd = ToCartesian(x, y, radius, a1 + dOut);
                var largeO = (sweep - 2 * dOut) >= 180 ? 1 : 0;

                if (isDonut)
                {
                    var dIn = Math.Asin(Math.Min(1, h / innerRadius)) * 180 / Math.PI;
                    var iEnd = ToCartesian(x, y, innerRadius, a1 + dIn);
                    var iStart = ToCartesian(x, y, innerRadius, a0 - dIn);
                    var largeI = (sweep - 2 * dIn) >= 180 ? 1 : 0;

                    return $"M {Format(oStart)} {Arc(radius, largeO, 1, oEnd)} L {Format(iEnd)} {Arc(innerRadius, largeI, 0, iStart)} Z";
                }

                var apex = ToCartesian(x, y, h / s, am);
                return $"M {Format(oStart)} {Arc(radius, largeO, 1, oEnd)} L {Format(apex)} Z";
            }

            // Gap with rounded corners. The fillets stay tangent to the arcs and to the inset edges; the only
            // change from the un-gapped case is that the fillet-center angular offset gains the inset h.
            var eOut = Math.Asin((h + rc) / (radius - rc));
            var eOutDeg = eOut * 180 / Math.PI;

            var outerArcStart = ToCartesian(x, y, radius, a0 - eOutDeg);
            var outerArcEnd = ToCartesian(x, y, radius, a1 + eOutDeg);
            var outerCenterStart = ToCartesian(x, y, radius - rc, a0 - eOutDeg);
            var outerCenterEnd = ToCartesian(x, y, radius - rc, a1 + eOutDeg);
            var outerEdgeStart = (outerCenterStart.X - rc * nStart.X, outerCenterStart.Y - rc * nStart.Y);
            var outerEdgeEnd = (outerCenterEnd.X - rc * nEnd.X, outerCenterEnd.Y - rc * nEnd.Y);

            var largeOuter = (sweep - 2 * eOutDeg) >= 180 ? 1 : 0;

            var path = $"M {Format(outerArcStart)}" +
                       $" {Arc(radius, largeOuter, 1, outerArcEnd)}" +
                       $" {Arc(rc, 0, Sweep(outerArcEnd, outerEdgeEnd, outerCenterEnd), outerEdgeEnd)}";

            if (isDonut)
            {
                var eIn = Math.Asin((h + rc) / (innerRadius + rc));
                var eInDeg = eIn * 180 / Math.PI;

                var innerArcStart = ToCartesian(x, y, innerRadius, a0 - eInDeg);
                var innerArcEnd = ToCartesian(x, y, innerRadius, a1 + eInDeg);
                var innerCenterStart = ToCartesian(x, y, innerRadius + rc, a0 - eInDeg);
                var innerCenterEnd = ToCartesian(x, y, innerRadius + rc, a1 + eInDeg);
                var innerEdgeStart = (innerCenterStart.X - rc * nStart.X, innerCenterStart.Y - rc * nStart.Y);
                var innerEdgeEnd = (innerCenterEnd.X - rc * nEnd.X, innerCenterEnd.Y - rc * nEnd.Y);

                var largeInner = (sweep - 2 * eInDeg) >= 180 ? 1 : 0;

                path += $" L {Format(innerEdgeEnd)}" +
                        $" {Arc(rc, 0, Sweep(innerEdgeEnd, innerArcEnd, innerCenterEnd), innerArcEnd)}" +
                        $" {Arc(innerRadius, largeInner, 0, innerArcStart)}" +
                        $" {Arc(rc, 0, Sweep(innerArcStart, innerEdgeStart, innerCenterStart), innerEdgeStart)}" +
                        $" L {Format(outerEdgeStart)}" +
                        $" {Arc(rc, 0, Sweep(outerEdgeStart, outerArcStart, outerCenterStart), outerArcStart)} Z";
            }
            else
            {
                // Pie: round only the outer corners; the inset edges meet at a sharp apex so the center stays
                // even across unequal slices (only the small gap recession remains, not the corner radius).
                var apex = ToCartesian(x, y, h / s, am);

                path += $" L {Format(apex)}" +
                        $" L {Format(outerEdgeStart)}" +
                        $" {Arc(rc, 0, Sweep(outerEdgeStart, outerArcStart, outerCenterStart), outerArcStart)} Z";
            }

            return path;
        }

        /// <summary>
        /// Returns the unit normal of the radial edge at <paramref name="edgeDegrees"/> pointing toward the
        /// segment interior (the bisector at <paramref name="midDegrees"/>).
        /// </summary>
        private (double X, double Y) InteriorNormal(double edgeDegrees, double midDegrees)
        {
            var edge = ToCartesian(0, 0, 1, edgeDegrees);
            var mid = ToCartesian(0, 0, 1, midDegrees);
            var normal = (X: -edge.Y, Y: edge.X);
            var dot = normal.X * mid.X + normal.Y * mid.Y;

            return dot >= 0 ? normal : (X: edge.Y, Y: -edge.X);
        }

        /// <summary>
        /// Formats a point as "x y" using the invariant culture.
        /// </summary>
        private static string Format((double X, double Y) point)
        {
            return $"{point.X.ToInvariantString()} {point.Y.ToInvariantString()}";
        }

        /// <summary>
        /// Builds an SVG elliptical-arc command to the given end point.
        /// </summary>
        private static string Arc(double radius, int largeArcFlag, int sweepFlag, (double X, double Y) end)
        {
            var r = radius.ToInvariantString();
            return $"A {r} {r} 0 {largeArcFlag} {sweepFlag} {Format(end)}";
        }

        /// <summary>
        /// Returns the SVG sweep flag for the minor arc from <paramref name="start"/> to <paramref name="end"/>
        /// about <paramref name="center"/>. In SVG (y-down) coordinates a positive cross product is drawn clockwise.
        /// </summary>
        private static int Sweep((double X, double Y) start, (double X, double Y) end, (double X, double Y) center)
        {
            var cross = (start.X - center.X) * (end.Y - center.Y) - (start.Y - center.Y) * (end.X - center.X);
            return cross > 0 ? 1 : 0;
        }

        /// <summary>
        /// Returns the inner radius used for data label placement - <c>0</c> for pie, the hole radius for donut.
        /// </summary>
        internal virtual double LabelInnerRadius(double outerRadius)
        {
            return 0;
        }

        /// <inheritdoc />
        public override IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY)
        {
            return GetDataLabels(offsetX, offsetY, DataLabelPosition.Auto);
        }

        /// <inheritdoc />
        public override IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY, DataLabelPosition position)
        {
            var list = new List<ChartDataLabel>();

            if(Data != null)
            {
                const double gap = 16;
                const double inset = 16;

                foreach (var d in PositiveItems)
                {
                    var x = TooltipX(d) - CenterX;
                    var y = TooltipY(d) - CenterY;

                    // find angle and add offset
                    var phi = Math.Atan2(y, x);

                    phi += Polar.ToRadian(offsetY % 360);

                    var radius = Math.Sqrt(x * x + y * y);

                    var (hyp, textAnchor) = position switch
                    {
                        DataLabelPosition.Inside => (radius - inset, "middle"),
                        DataLabelPosition.Center => (LabelInnerRadius(radius) + (radius - LabelInnerRadius(radius)) / 2, "middle"),
                        _ => (radius + gap, phi >= -1.5 && phi <= 1.5 ? "start" : "end"),
                    };

                    hyp += offsetX;

                    var anchorX = CenterX + radius * Math.Cos(phi);
                    var anchorY = CenterY + radius * Math.Sin(phi);

                    // move along the radius and rotate
                    x = CenterX + hyp * Math.Cos(phi);
                    y = CenterY + hyp * Math.Sin(phi);

                    var chart = RequireChart();
                    if (chart != null)
                    {
                        list.Add(new ChartDataLabel
                        {
                            TextAnchor = textAnchor,
                            Position = new Point { X = x, Y = y },
                            Anchor = new Point { X = anchorX, Y = anchorY },
                            Value = Value(d),
                            Text = chart.ValueAxis.Format(chart.ValueScale, Value(d))
                        });
                    }
                }
            }

            return list;
        }
    }
}
