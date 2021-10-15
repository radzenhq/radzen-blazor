using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

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
        /// Gets the current X coordiante of the center.
        /// </summary>
        protected double CenterX
        {
            get
            {
                return X ?? Chart.CategoryScale.Output.Mid + 8;
            }
        }

        /// <summary>
        /// Gets the current Y coordiante of the center.
        /// </summary>
        protected double CenterY
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
                    builder.CloseComponent();
                };
            };
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
        public override object DataAt(double x, double y)
        {
            var angle = 90 - Math.Atan((CenterY - y) / (x - CenterX)) * 180 / Math.PI;

            if (x < CenterX)
            {
                angle += 180;
            }

            var sum = Items.Sum(Value);
            double startAngle = 0;

            foreach (var data in Items)
            {
                var value = Value(data);
                var endAngle = startAngle + (value / sum) * 360;

                if (startAngle <= angle && angle <= endAngle)
                {
                    return data;
                }

                startAngle = endAngle;
            }

            return null;
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

            var color = PickColor(Items.IndexOf(item), Fills);

            if (color != null)
            {
                style = $"{style}; border-color: {color};";
            }

            return style;
        }

        /// <inheritdoc />
        protected override double TooltipX(TItem item)
        {
            var sum = Items.Sum(Value);
            double startAngle = 0;
            double endAngle = 0;

            foreach (var data in Items)
            {
                var value = Value(data);
                endAngle = startAngle + (value / sum) * 360;

                if (EqualityComparer<TItem>.Default.Equals(data, item))
                {
                    break;
                }

                startAngle = endAngle;
            }

            var angle = startAngle + (endAngle - startAngle) / 2;

            return CenterX + CurrentRadius * Math.Cos(DegToRad(90 - angle));
        }

        /// <inheritdoc />
        protected override double TooltipY(TItem item)
        {
            var sum = Items.Sum(Value);
            double startAngle = 0;
            double endAngle = 0;

            foreach (var data in Items)
            {
                var value = Value(data);
                endAngle = startAngle + (value / sum) * 360;

                if (EqualityComparer<TItem>.Default.Equals(data, item))
                {
                    break;
                }

                startAngle = endAngle;
            }

            var angle = startAngle + (endAngle - startAngle) / 2;

            return CenterY - CurrentRadius * Math.Sin(DegToRad(90 - angle));
        }

        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        protected double DegToRad(double degrees)
        {
            var radians = (degrees) * Math.PI / 180;

            return radians;
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
            var radians = (degrees) * Math.PI / 180;

            return (x + radius * Math.Cos(radians), y + radius * Math.Sin(radians));
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
            var start = ToCartesian(x, y, radius, startAngle);
            var end = ToCartesian(x, y, radius, endAngle);

            var innerStart = ToCartesian(x, y, innerRadius, startAngle);
            var innerEnd = ToCartesian(x, y, innerRadius, endAngle);

            var largeArcFlag = endAngle - startAngle <= 180 ? 0 : 1;
            var startX = start.X.ToInvariantString();
            var startY = start.Y.ToInvariantString();
            var endX = end.X.ToInvariantString();
            var endY = end.Y.ToInvariantString();
            var r = radius.ToInvariantString();
            var innerStartX = innerStart.X.ToInvariantString();
            var innerStartY = innerStart.Y.ToInvariantString();
            var innerEndX = innerEnd.X.ToInvariantString();
            var innerEndY = innerEnd.Y.ToInvariantString();

            if (Math.Abs(end.X - start.X) < 0.01 && Math.Abs(end.Y - start.Y) < 0.01)
            {
                // Full circle - SVG can't render a full circle arc 
                endX = (end.X - 0.01).ToInvariantString();

                innerEndX = (innerEnd.X - 0.01).ToInvariantString();
            }

            return $"M {startX} {startY} A {r} {r} 0 {largeArcFlag} 1 {endX} {endY} L {innerEndX} {innerEndY} A {innerRadius} {innerRadius} 0 {largeArcFlag} 0 {innerStartX} {innerStartY}";
        }
    }
}