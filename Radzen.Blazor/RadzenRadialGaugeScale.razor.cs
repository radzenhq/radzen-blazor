using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenRadialGaugeScale component.
    /// </summary>
    public partial class RadzenRadialGaugeScale : ComponentBase
    {
        /// <summary>
        /// Gets or sets the gauge.
        /// </summary>
        /// <value>The gauge.</value>
        [CascadingParameter]
        public RadzenRadialGauge Gauge { get; set; }

        /// <summary>
        /// Gets or sets the stroke.
        /// </summary>
        /// <value>The stroke.</value>
        [Parameter]
        public string Stroke { get; set; }

        /// <summary>
        /// Gets or sets the width of the stroke.
        /// </summary>
        /// <value>The width of the stroke.</value>
        [Parameter]
        public double StrokeWidth { get; set; } = 1;

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the length of the tick.
        /// </summary>
        /// <value>The length of the tick.</value>
        [Parameter]
        public double TickLength { get; set; } = 10;

        /// <summary>
        /// Gets or sets the length of the minor tick.
        /// </summary>
        /// <value>The length of the minor tick.</value>
        [Parameter]
        public double MinorTickLength { get; set; } = 5;

        /// <summary>
        /// Gets or sets the tick label offset.
        /// </summary>
        /// <value>The tick label offset.</value>
        [Parameter]
        public double TickLabelOffset { get; set; } = 25;

        /// <summary>
        /// Gets or sets the format string.
        /// </summary>
        /// <value>The format string.</value>
        [Parameter]
        public string FormatString { get; set; }

        /// <summary>
        /// Gets or sets the formatter function.
        /// </summary>
        /// <value>The formatter function.</value>
        [Parameter]
        public Func<double, string> Formatter { get; set; } = value => value.ToString();

        /// <summary>
        /// Gets or sets the start angle.
        /// </summary>
        /// <value>The start angle.</value>
        [Parameter]
        public double StartAngle { get; set; } = -90;

        /// <summary>
        /// Gets or sets the tick position.
        /// </summary>
        /// <value>The tick position.</value>
        [Parameter]
        public GaugeTickPosition TickPosition { get; set; } = GaugeTickPosition.Outside;

        /// <summary>
        /// Gets or sets the end angle.
        /// </summary>
        /// <value>The end angle.</value>
        [Parameter]
        public double EndAngle { get; set; } = 90;

        /// <summary>
        /// Gets or sets the radius.
        /// </summary>
        /// <value>The radius.</value>
        [Parameter]
        public double Radius { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value indicating whether to show first tick.
        /// </summary>
        /// <value><c>true</c> if first tick is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowFirstTick { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show last tick.
        /// </summary>
        /// <value><c>true</c> if last tick is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowLastTick { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether to show tick labels.
        /// </summary>
        /// <value><c>true</c> if tick labels are shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowTickLabels { get; set; } = true;

        /// <summary>
        /// Gets or sets the x.
        /// </summary>
        /// <value>The x.</value>
        [Parameter]
        public double X { get; set; } = 0.5;

        /// <summary>
        /// Determines the minimum value.
        /// </summary>
        /// <value>The minimum value.</value>
        [Parameter]
        public double Min { get; set; } = 0;

        /// <summary>
        /// Determines the maximum value.
        /// </summary>
        /// <value>The maximum value.</value>
        [Parameter]
        public double Max { get; set; } = 100;

        /// <summary>
        /// Gets or sets the step.
        /// </summary>
        /// <value>The step.</value>
        [Parameter]
        public double Step { get; set; } = 20;

        /// <summary>
        /// Gets or sets the minor step.
        /// </summary>
        /// <value>The minor step.</value>
        [Parameter]
        public double MinorStep { get; set; }

        /// <summary>
        /// Gets or sets the y.
        /// </summary>
        /// <value>The y.</value>
        [Parameter]
        public double Y { get; set; } = 0.5;

        /// <summary>
        /// Gets or sets the margin.
        /// </summary>
        /// <value>The margin.</value>
        [Parameter]
        public double Margin { get; set; } = 16;

        /// <summary>
        /// Gets the current radius.
        /// </summary>
        /// <value>The current radius.</value>
        public double CurrentRadius
        {
            get
            {
                var radius = Math.Min(Gauge.Width.Value, Gauge.Height.Value) / 2 - Margin * 2;

                radius *= Radius;

                if (TickPosition == GaugeTickPosition.Outside)
                {
                    radius -= TextMeasurer.TextWidth(Max.ToString(), 16);
                }

                return radius;
            }
        }

        /// <summary>
        /// Gets the current center.
        /// </summary>
        /// <value>The current center.</value>
        public Point CurrentCenter
        {
            get
            {
                var x = X * Gauge.Width;
                var y = Y * Gauge.Height;

                return new Point { X = x.Value, Y = y.Value };
            }
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var shouldRefresh = false;

            if (parameters.DidParameterChange(nameof(X), X) || parameters.DidParameterChange(nameof(Y), Y))
            {
                shouldRefresh = true;
            }

            await base.SetParametersAsync(parameters);

            if (shouldRefresh)
            {
                Gauge.Reload();
            }
        }
    }
}
