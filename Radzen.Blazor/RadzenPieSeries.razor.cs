using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
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
        /// Gets or sets a collection of fill colors applied to individual pie segments in sequence.
        /// Each segment gets the color at its index position. If fewer colors than segments, colors are reused cyclically.
        /// If not set, uses the chart's color scheme.
        /// </summary>
        /// <value>An enumerable collection of CSS color values for segment backgrounds.</value>
        [Parameter]
        public IEnumerable<string> Fills { get; set; }

        /// <summary>
        /// Gets or sets a collection of stroke (border) colors applied to individual pie segments in sequence.
        /// Use with <see cref="StrokeWidth"/> to create visible segment borders.
        /// </summary>
        /// <value>An enumerable collection of CSS color values for segment borders.</value>
        [Parameter]
        public IEnumerable<string> Strokes { get; set; }

        /// <summary>
        /// Gets or sets the width of the pie segment borders in pixels.
        /// Set to 0 for no borders, or increase to make segment divisions more visible.
        /// </summary>
        /// <value>The stroke width in pixels. Default is 0 (no borders).</value>
        [Parameter]
        public double StrokeWidth { get; set; }

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
                return Radius ?? Math.Min(Chart.CategoryScale.Output.Mid, Chart.ValueScale.Output.Mid);
            }
        }

        /// <summary>
        /// Gets the current X coordinate of the center.
        /// </summary>
        internal double CenterX
        {
            get
            {
                return X ?? Chart.CategoryScale.Output.Mid + 8;
            }
        }

        /// <summary>
        /// Gets the current Y coordinate of the center.
        /// </summary>
        internal double CenterY
        {
            get
            {
                return Y ?? Chart.ValueScale.Output.Mid;
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
                    builder.AddAttribute(6, nameof(LegendItem.Click), EventCallback.Factory.Create(this, () => OnLegendClick(data)));
                    builder.AddAttribute(7, nameof(LegendItem.Clickable), clickable);
                    builder.CloseComponent();
                };
            };
        }

        private async Task OnLegendClick(object data)
        {
            if (Chart.LegendClick.HasDelegate)
            {
                var args = new LegendClickEventArgs
                {
                    Data = data,
                    Title = GetTitle(),
                    IsVisible = true,
                };

                await Chart.LegendClick.InvokeAsync(args);
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

        /// <inheritdoc />
        public override (object, Point) DataAt(double x, double y)
        {
            if (!Contains(x, y, 0))
            {
                return (null, null);
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
                    return (data, new Point() { X = x, Y = y });
                }

                startAngle = endAngle;
            }

            return (null, null);
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

            return CenterX + CurrentRadius * Math.Cos(DegToRad(angle));
        }

        /// <inheritdoc />
        internal override double TooltipY(TItem item)
        {
            var angle = TooltipAngle(item);

            return CenterY - CurrentRadius * Math.Sin(DegToRad(angle));
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
        /// Creates SVG path that renders the specified segment.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="innerRadius">The inner radius.</param>
        /// <param name="startAngle">The start angle.</param>
        /// <param name="endAngle">The end angle.</param>
        protected string Segment(double x, double y, double radius, double innerRadius, double startAngle, double endAngle)
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

        /// <inheritdoc />
        public override IEnumerable<ChartDataLabel> GetDataLabels(double offsetX, double offsetY)
        {
            var list = new List<ChartDataLabel>();

            if(Data != null)
            {
                foreach (var d in PositiveItems)
                {
                    var x = TooltipX(d) - CenterX;
                    var y = TooltipY(d) - CenterY;

                    // find angle and add offset
                    var phi = Math.Atan2(y, x);

                    phi += Polar.ToRadian(offsetY % 360);

                    var textAnchor = phi >= -1.5 && phi <= 1.5 ? "start" : "end";

                    // find radius
                    var hyp = Math.Sqrt(x * x + y * y) + offsetX + 16;

                    // move along the radius and rotate
                    x = CenterX + hyp * Math.Cos(phi);
                    y = CenterY + hyp * Math.Sin(phi);

                    list.Add(new ChartDataLabel
                    {
                        TextAnchor = textAnchor,
                        Position = new Point { X = x, Y = y },
                        Text = Chart.ValueAxis.Format(Chart.ValueScale, Value(d))
                    });
                }
            }

            return list;
        }
    }
}
