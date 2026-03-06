using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Renders the cartesian scale, major ticks, minor ticks, and labels for the linear gauge scale component.
    /// </summary>
    public partial class LinearGaugeScaleRenderer : ComponentBase
    {
        /// <summary>
        /// Gets or sets the start point of the scale line.
        /// </summary>
        [Parameter]
        public Point Start { get; set; } = new Point();

        /// <summary>
        /// Gets or sets the end point of the scale line.
        /// </summary>
        [Parameter]
        public Point End { get; set; } = new Point();

        /// <summary>
        /// Gets or sets the scale orientation.
        /// </summary>
        [Parameter]
        public global::Radzen.Orientation Orientation { get; set; }

        /// <summary>
        /// Gets or sets the scale stroke color.
        /// </summary>
        [Parameter]
        public string Stroke { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the width of the scale stroke.
        /// </summary>
        [Parameter]
        public double StrokeWidth { get; set; }

        /// <summary>
        /// Gets or sets the major tick length in pixels.
        /// </summary>
        [Parameter]
        public double TickLength { get; set; }

        /// <summary>
        /// Gets or sets the minor tick length in pixels.
        /// </summary>
        [Parameter]
        public double MinorTickLength { get; set; }

        /// <summary>
        /// Gets or sets the offset between the scale line and tick labels.
        /// </summary>
        [Parameter]
        public double TickLabelOffset { get; set; }

        /// <summary>
        /// Gets or sets the minimum scale value.
        /// </summary>
        [Parameter]
        public double Min { get; set; }

        /// <summary>
        /// Gets or sets the maximum scale value.
        /// </summary>
        [Parameter]
        public double Max { get; set; }

        /// <summary>
        /// Gets or sets whether the first major tick is rendered.
        /// </summary>
        [Parameter]
        public bool ShowFirstTick { get; set; }

        /// <summary>
        /// Gets or sets whether the last major tick is rendered.
        /// </summary>
        [Parameter]
        public bool ShowLastTick { get; set; }

        /// <summary>
        /// Gets or sets whether major tick labels are rendered.
        /// </summary>
        [Parameter]
        public bool ShowTickLabels { get; set; }

        /// <summary>
        /// Gets or sets the tick position relative to the scale line.
        /// </summary>
        [Parameter]
        public GaugeTickPosition TickPosition { get; set; }

        /// <summary>
        /// Gets or sets the format string used for major tick labels.
        /// </summary>
        [Parameter]
        public string? FormatString { get; set; }

        /// <summary>
        /// Gets or sets the formatter used for major tick labels.
        /// </summary>
        [Parameter]
        public Func<double, string> Formatter { get; set; } = value => value.ToInvariantString();

        /// <summary>
        /// Gets or sets the major tick interval.
        /// </summary>
        [Parameter]
        public double Step { get; set; }

        /// <summary>
        /// Gets or sets the minor tick interval.
        /// </summary>
        [Parameter]
        public double MinorStep { get; set; }

        /// <summary>
        /// Gets or sets whether the scale is rendered in right-to-left mode.
        /// When <c>true</c>, tick labels for vertical scales appear on the left side instead of the right.
        /// </summary>
        [Parameter]
        public bool IsRTL { get; set; }

        private IList<TickModel> Ticks { get; set; } = new List<TickModel>();
        private IList<TickModel> MinorTicks { get; set; } = new List<TickModel>();

        private string ScaleStyle { get; set; } = string.Empty;

        private class TickModel
        {
            /// <summary>
            /// Gets or sets the tick start point.
            /// </summary>
            public Point Start { get; set; } = new Point();

            /// <summary>
            /// Gets or sets the tick end point.
            /// </summary>
            public Point End { get; set; } = new Point();

            /// <summary>
            /// Gets or sets the label position.
            /// </summary>
            public Point Text { get; set; } = new Point();

            /// <summary>
            /// Gets or sets the formatted tick label text.
            /// </summary>
            public string? Value { get; set; }

            /// <summary>
            /// Gets or sets the SVG text anchor value for the label.
            /// </summary>
            public string TextAnchor { get; set; } = "middle";
        }

        private Point Interpolate(double ratio)
        {
            return new Point
            {
                X = Start.X + (End.X - Start.X) * ratio,
                Y = Start.Y + (End.Y - Start.Y) * ratio
            };
        }

        private TickModel CreateTick(double ratio, double length, string? value = null)
        {
            var point = Interpolate(ratio);
            var tick = new TickModel { Value = value };

            if (Orientation == global::Radzen.Orientation.Vertical)
            {
                var direction = (TickPosition == GaugeTickPosition.Outside) != IsRTL ? 1 : -1;
                tick.Start = point;
                tick.End = new Point { X = point.X + direction * length, Y = point.Y };
                tick.Text = new Point { X = point.X + direction * TickLabelOffset, Y = point.Y };
                tick.TextAnchor = direction > 0 ? "start" : "end";
            }
            else
            {
                var direction = TickPosition == GaugeTickPosition.Outside ? 1 : -1;
                tick.Start = point;
                tick.End = new Point { X = point.X, Y = point.Y + direction * length };
                tick.Text = new Point { X = point.X, Y = point.Y + direction * TickLabelOffset };
                tick.TextAnchor = "middle";
            }

            return tick;
        }

        /// <inheritdoc />
        protected override void OnParametersSet()
        {
            var parts = new List<string>(2);
            if (!string.IsNullOrEmpty(Stroke))
            {
                parts.Add($"stroke: {Stroke}");
            }
            if (StrokeWidth > 0)
            {
                parts.Add($"stroke-width: {StrokeWidth.ToInvariantString()}");
            }
            ScaleStyle = string.Join("; ", parts);

            Ticks = new List<TickModel>();
            MinorTicks = new List<TickModel>();

            if (TickPosition == GaugeTickPosition.None || Step <= 0 || Max <= Min)
            {
                return;
            }

            var count = Math.Ceiling((Max - Min) / Step);

            for (var idx = 0; idx <= count; idx++)
            {
                var value = Math.Min(Min + idx * Step, Max);
                var ratio = (value - Min) / (Max - Min);

                if (idx == 0 && ShowFirstTick == false)
                {
                    continue;
                }

                if (idx == count && ShowLastTick == false)
                {
                    continue;
                }

                string? text = null;
                if (ShowTickLabels)
                {
                    text = !string.IsNullOrEmpty(FormatString) ? string.Format(CultureInfo.InvariantCulture, FormatString, value) : Formatter(value);
                }

                Ticks.Add(CreateTick(ratio, TickLength, text));
            }

            if (MinorStep > 0 && MinorStep < Step)
            {
                var minorCount = Math.Floor((Max - Min) / MinorStep);
                for (var idx = 0; idx <= minorCount; idx++)
                {
                    var value = Math.Min(Min + idx * MinorStep, Max);
                    var ratio = (value - Min) / (Max - Min);
                    var majorIndex = (value - Min) / Step;

                    if (Math.Abs(majorIndex - Math.Round(majorIndex)) < 0.0001)
                    {
                        continue;
                    }

                    MinorTicks.Add(CreateTick(ratio, MinorTickLength));
                }
            }
        }
    }
}
