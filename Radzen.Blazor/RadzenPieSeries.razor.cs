using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenPieSeries.
    /// Implements the <see cref="Radzen.Blazor.CartesianSeries{TItem}" />
    /// </summary>
    /// <typeparam name="TItem">The type of the t item.</typeparam>
    /// <seealso cref="Radzen.Blazor.CartesianSeries{TItem}" />
    public partial class RadzenPieSeries<TItem> : Radzen.Blazor.CartesianSeries<TItem>
    {
        /// <summary>
        /// Gets or sets the x.
        /// </summary>
        /// <value>The x.</value>
        [Parameter]
        public double? X { get; set; }

        /// <summary>
        /// Gets or sets the y.
        /// </summary>
        /// <value>The y.</value>
        [Parameter]
        public double? Y { get; set; }

        /// <summary>
        /// Gets or sets the radius.
        /// </summary>
        /// <value>The radius.</value>
        [Parameter]
        public double? Radius { get; set; }

        /// <summary>
        /// Gets or sets the fills.
        /// </summary>
        /// <value>The fills.</value>
        [Parameter]
        public IEnumerable<string> Fills { get; set; }

        /// <summary>
        /// Gets or sets the strokes.
        /// </summary>
        /// <value>The strokes.</value>
        [Parameter]
        public IEnumerable<string> Strokes { get; set; }

        /// <summary>
        /// Gets or sets the width of the stroke.
        /// </summary>
        /// <value>The width of the stroke.</value>
        [Parameter]
        public double StrokeWidth { get; set; }

        /// <summary>
        /// Gets the current radius.
        /// </summary>
        /// <value>The current radius.</value>
        protected double CurrentRadius
        {
            get
            {
                return Radius ?? Math.Min(Chart.CategoryScale.Output.Mid, Chart.ValueScale.Output.Mid);
            }
        }

        /// <summary>
        /// Gets the center x.
        /// </summary>
        /// <value>The center x.</value>
        protected double CenterX
        {
            get
            {
                return X ?? Chart.CategoryScale.Output.Mid + 8;
            }
        }

        /// <summary>
        /// Gets the center y.
        /// </summary>
        /// <value>The center y.</value>
        protected double CenterY
        {
            get
            {
                return Y ?? Chart.ValueScale.Output.Mid;
            }
        }
        /// <summary>
        /// Gets the color.
        /// </summary>
        /// <value>The color.</value>
        public override string Color
        {
            get
            {
                return "#000";
            }
        }

        /// <summary>
        /// Measures the legend.
        /// </summary>
        /// <returns>System.Double.</returns>
        public override double MeasureLegend()
        {
            if (Items.Any())
            {
                return Items.Select(item => TextMeasurer.TextWidth(TooltipTitle(item))).Max() + MarkerSize;
            }

            return 0;
        }

        /// <summary>
        /// Renders the legend item.
        /// </summary>
        /// <returns>RenderFragment.</returns>
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

        /// <summary>
        /// Determines whether this instance contains the object.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="tolerance">The tolerance.</param>
        /// <returns><c>true</c> if [contains] [the specified x]; otherwise, <c>false</c>.</returns>
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
        /// Datas at.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <returns>System.Object.</returns>
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
        /// <summary>
        /// Tooltips the class.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
        protected override string TooltipClass(TItem item)
        {
            return $"{base.TooltipClass(item)} rz-pie-tooltip rz-series-item-{Items.IndexOf(item)}";
        }
        /// <summary>
        /// Tooltips the style.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.String.</returns>
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

        /// <summary>
        /// Tooltips the x.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.Double.</returns>
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

        /// <summary>
        /// Tooltips the y.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>System.Double.</returns>
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
        /// Degs to RAD.
        /// </summary>
        /// <param name="degrees">The degrees.</param>
        /// <returns>System.Double.</returns>
        protected double DegToRad(double degrees)
        {
            var radians = (degrees) * Math.PI / 180;

            return radians;
        }

        /// <summary>
        /// Converts to cartesian.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="degrees">The degrees.</param>
        /// <returns>System.ValueTuple&lt;System.Double, System.Double&gt;.</returns>
        protected (double X, double Y) ToCartesian(double x, double y, double radius, double degrees)
        {
            var radians = (degrees) * Math.PI / 180;

            return (x + radius * Math.Cos(radians), y + radius * Math.Sin(radians));
        }

        /// <summary>
        /// Segments the specified x.
        /// </summary>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="innerRadius">The inner radius.</param>
        /// <param name="startAngle">The start angle.</param>
        /// <param name="endAngle">The end angle.</param>
        /// <returns>System.String.</returns>
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