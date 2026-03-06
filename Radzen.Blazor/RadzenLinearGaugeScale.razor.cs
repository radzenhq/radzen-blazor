using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenLinearGaugeScale component.
    /// </summary>
    public partial class RadzenLinearGaugeScale : ComponentBase
    {
        /// <summary>
        /// Gets or sets the parent linear gauge.
        /// </summary>
        [CascadingParameter]
        public RadzenLinearGauge? Gauge { get; set; }

        /// <summary>
        /// Gets or sets the scale stroke color.
        /// </summary>
        [Parameter]
        public string Stroke { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the width of the scale stroke.
        /// </summary>
        [Parameter]
        public double StrokeWidth { get; set; } = 1;

        /// <summary>
        /// Gets or sets the child content containing pointers and ranges.
        /// </summary>
        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the major tick length in pixels.
        /// </summary>
        [Parameter]
        public double TickLength { get; set; } = 10;

        /// <summary>
        /// Gets or sets the minor tick length in pixels.
        /// </summary>
        [Parameter]
        public double MinorTickLength { get; set; } = 5;

        /// <summary>
        /// Gets or sets the offset between the scale line and the tick labels.
        /// </summary>
        [Parameter]
        public double TickLabelOffset { get; set; } = 25;

        /// <summary>
        /// Gets or sets the format string used for tick labels.
        /// </summary>
        [Parameter]
        public string? FormatString { get; set; }

        /// <summary>
        /// Gets or sets the formatter used for tick labels.
        /// </summary>
        [Parameter]
        public Func<double, string> Formatter { get; set; } = value => value.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Gets or sets the tick position relative to the scale line.
        /// </summary>
        [Parameter]
        public GaugeTickPosition TickPosition { get; set; } = GaugeTickPosition.Outside;

        /// <summary>
        /// Gets or sets whether the first major tick is rendered.
        /// </summary>
        [Parameter]
        public bool ShowFirstTick { get; set; } = true;

        /// <summary>
        /// Gets or sets whether the last major tick is rendered.
        /// </summary>
        [Parameter]
        public bool ShowLastTick { get; set; } = true;

        /// <summary>
        /// Gets or sets whether major tick labels are rendered.
        /// </summary>
        [Parameter]
        public bool ShowTickLabels { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum scale value.
        /// </summary>
        [Parameter]
        public double Min { get; set; } = 0;

        /// <summary>
        /// Gets or sets the maximum scale value.
        /// </summary>
        [Parameter]
        public double Max { get; set; } = 100;

        /// <summary>
        /// Gets or sets the major tick interval.
        /// </summary>
        [Parameter]
        public double Step { get; set; } = 20;

        /// <summary>
        /// Gets or sets the minor tick interval.
        /// </summary>
        [Parameter]
        public double MinorStep { get; set; }

        /// <summary>
        /// Gets or sets the outer margin used to lay out the scale within the gauge.
        /// </summary>
        [Parameter]
        public double Margin { get; set; } = 16;

        /// <summary>
        /// Gets or sets the linear gauge orientation.
        /// </summary>
        [Parameter]
        public global::Radzen.Orientation Orientation { get; set; } = global::Radzen.Orientation.Horizontal;

        /// <summary>
        /// Gets or sets the position of the scale line along the cross axis as a fraction of the gauge dimension (0.0–1.0).
        /// When <c>null</c> (default) the position is chosen automatically based on <see cref="TickPosition" />.
        /// </summary>
        [Parameter]
        public double? LinePosition { get; set; }

        /// <summary>
        /// Gets or sets whether the scale direction is reversed (Max at start, Min at end).
        /// For horizontal gauges this puts Max on the left; for vertical it puts Max at the bottom.
        /// </summary>
        [Parameter]
        public bool Reversed { get; set; }

        /// <summary>
        /// Gets whether the scale is in a right-to-left layout context.
        /// Automatically propagated from the parent <see cref="RadzenLinearGauge"/>.
        /// </summary>
        internal bool IsRTL => Gauge?.IsRTL ?? false;

        /// <summary>
        /// Gets or sets a callback invoked when the user clicks on the scale area.
        /// The argument is the computed scale value at the clicked position.
        /// </summary>
        [Parameter]
        public EventCallback<double> Click { get; set; }

        private Point NaturalStart
        {
            get
            {
                var gauge = Gauge;
                if (gauge?.Width == null || gauge.Height == null) return new Point();

                if (Orientation == global::Radzen.Orientation.Vertical)
                {
                    return new Point { X = ScaleX(gauge), Y = gauge.Height.Value - Margin };
                }
                return new Point { X = Margin, Y = ScaleY(gauge) };
            }
        }

        private Point NaturalEnd
        {
            get
            {
                var gauge = Gauge;
                if (gauge?.Width == null || gauge.Height == null) return new Point();

                if (Orientation == global::Radzen.Orientation.Vertical)
                {
                    return new Point { X = ScaleX(gauge), Y = Margin };
                }
                return new Point { X = gauge.Width.Value - Margin, Y = ScaleY(gauge) };
            }
        }

        // For horizontal gauges in RTL mode the natural direction is right-to-left,
        // which is equivalent to Reversed. XOR: RTL flips once, Reversed flips once; both together cancel out.
        private bool IsEffectivelyReversed => Reversed != (IsRTL && Orientation == global::Radzen.Orientation.Horizontal);

        /// <summary>
        /// Gets the start point of the rendered scale line (accounts for <see cref="Reversed" /> and RTL).
        /// </summary>
        public Point CurrentStart => IsEffectivelyReversed ? NaturalEnd : NaturalStart;

        /// <summary>
        /// Gets the end point of the rendered scale line (accounts for <see cref="Reversed" /> and RTL).
        /// </summary>
        public Point CurrentEnd => IsEffectivelyReversed ? NaturalStart : NaturalEnd;

        private double ScaleY(RadzenLinearGauge gauge)
        {
            var h = gauge.Height!.Value;

            if (LinePosition.HasValue)
            {
                return h * LinePosition.Value;
            }

            if (TickPosition == GaugeTickPosition.None)
            {
                return h / 2;
            }

            if (TickPosition == GaugeTickPosition.Inside)
            {
                return h * 0.6;
            }

            return h * 0.4;
        }

        private double ScaleX(RadzenLinearGauge gauge)
        {
            var w = gauge.Width!.Value;

            if (LinePosition.HasValue)
            {
                return w * LinePosition.Value;
            }

            if (TickPosition == GaugeTickPosition.None)
            {
                return w / 2;
            }

            // In RTL the tick side flips, so the scale line moves to the opposite side to keep label room.
            var ticksGoRight = (TickPosition == GaugeTickPosition.Outside) != IsRTL;
            return w * (ticksGoRight ? 0.65 : 0.35);
        }

        internal Point ValueToPoint(double value)
        {
            var ratio = Max == Min ? 0 : (Clip(value) - Min) / (Max - Min);

            if (Orientation == global::Radzen.Orientation.Vertical)
            {
                return new Point
                {
                    X = CurrentStart.X,
                    Y = CurrentStart.Y + (CurrentEnd.Y - CurrentStart.Y) * ratio
                };
            }

            return new Point
            {
                X = CurrentStart.X + (CurrentEnd.X - CurrentStart.X) * ratio,
                Y = CurrentStart.Y
            };
        }

        internal double Clip(double value)
        {
            return Math.Max(Min, Math.Min(value, Max));
        }

        internal void HandleScaleClick(MouseEventArgs args)
        {
            if (!Click.HasDelegate) return;

            double svgX, svgY;
            if (Orientation == global::Radzen.Orientation.Vertical)
            {
                // Overlay rect top = Math.Min(CurrentStart.Y, CurrentEnd.Y); offsetY is from rect top.
                svgX = CurrentStart.X;
                svgY = Math.Min(CurrentStart.Y, CurrentEnd.Y) + args.OffsetY;
            }
            else
            {
                // Overlay rect left = Math.Min(CurrentStart.X, CurrentEnd.X); offsetX is from rect left.
                svgX = Math.Min(CurrentStart.X, CurrentEnd.X) + args.OffsetX;
                svgY = CurrentStart.Y;
            }

            Click.InvokeAsync(PointToValue(svgX, svgY));
        }

        // Converts an SVG coordinate to a clamped scale value. Works for both normal and Reversed scales.
        private double PointToValue(double svgX, double svgY)
        {
            double ratio;
            if (Orientation == global::Radzen.Orientation.Vertical)
            {
                var length = CurrentEnd.Y - CurrentStart.Y;
                ratio = Math.Abs(length) > 0 ? (svgY - CurrentStart.Y) / length : 0;
            }
            else
            {
                var length = CurrentEnd.X - CurrentStart.X;
                ratio = Math.Abs(length) > 0 ? (svgX - CurrentStart.X) / length : 0;
            }
            return Clip(Min + ratio * (Max - Min));
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var shouldRefresh =
                parameters.DidParameterChange(nameof(Orientation), Orientation) ||
                parameters.DidParameterChange(nameof(Min), Min) ||
                parameters.DidParameterChange(nameof(Max), Max) ||
                parameters.DidParameterChange(nameof(Margin), Margin) ||
                parameters.DidParameterChange(nameof(LinePosition), LinePosition) ||
                parameters.DidParameterChange(nameof(Reversed), Reversed);

            await base.SetParametersAsync(parameters);

            if (shouldRefresh && Gauge != null)
            {
                Gauge.Reload();
            }
        }
    }
}
