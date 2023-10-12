using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Renders pie series in <see cref="RadzenChart" />.
    /// </summary>
    /// <typeparam name="TItem">The type of the series data item.</typeparam>
    public partial class RadzenPieSeries<TItem> : CartesianSeries<TItem>
    {
        /// <summary>
        /// Specifies the x coordinate of the pie center. Not set by default which centers pie horizontally.
        /// </summary>
        /// <value>The x.</value>
        [Parameter]
        public double? X { get; set; }

        /// <summary>
        /// Specifies the y coordinate of the pie center. Not set by default which centers pie vertically.
        /// </summary>
        /// <value>The y.</value>
        [Parameter]
        public double? Y { get; set; }

        /// <summary>
        /// Specifies the radius of the pie. Not set by default - the pie takes as much size of the chart as possible.
        /// </summary>
        [Parameter]
        public double? Radius { get; set; }

        /// <summary>
        /// The fill colors of the pie segments. Used as the background of the segments.
        /// </summary>
        [Parameter]
        public IEnumerable<string> Fills { get; set; }

        /// <summary>
        /// The stroke colors of the pie segments.
        /// </summary>
        [Parameter]
        public IEnumerable<string> Strokes { get; set; }

        /// <summary>
        /// The stroke width of the segments in pixels. By default set to <c>0</c>.
        /// </summary>
        /// <value>The width of the stroke.</value>
        [Parameter]
        public double StrokeWidth { get; set; }

        /// <summary>
        /// Gets or sets the start angle from which segments are rendered (clockwise). Set to <c>90</c> by default.
        /// Top is <c>90</c>, right is <c>0</c>, bottom is <c>270</c>, left is <c>180</c>.
        /// </summary>
        [Parameter]
        public double StartAngle { get; set; } = 90;

        /// <summary>
        /// Gets or sets the total angle of the pie in degrees. Set to <c>360</c> by default which renders a full circle.
        /// Set to <c>180</c> to render a half circle or
        /// </summary>
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
        protected IList<TItem> PositiveItems { get; set; }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            await base.SetParametersAsync(parameters);

            if (Items != null)
            {
                PositiveItems = Items.Where(e => Value(e) > 0).ToList();
            }
            else
            {
                PositiveItems = new List<TItem>();
            }
        }

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
        public override RenderFragment RenderLegendItem()
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
            var sum = PositiveItems.Sum(Value);
            var startAngle = StartAngle;
            var endAngle = 0d;

            foreach (var data in PositiveItems)
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
