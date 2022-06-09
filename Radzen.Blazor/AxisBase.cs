using System;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Base class for an axis in <see cref="RadzenChart" />.
    /// </summary>
    public abstract class AxisBase : RadzenChartComponentBase, IChartAxis
    {
        /// <summary>
        /// Gets or sets the stroke (line color) of the axis.
        /// </summary>
        /// <value>The stroke.</value>
        [Parameter]
        public string Stroke { get; set; }
        /// <summary>
        /// Gets or sets the pixel width of axis.
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
        /// Gets or sets the format string used to display the axis values.
        /// </summary>
        /// <value>The format string.</value>
        [Parameter]
        public string FormatString { get; set; }

        /// <summary>
        /// Gets or sets a formatter function that formats the axis values.
        /// </summary>
        /// <value>The formatter.</value>
        [Parameter]
        public Func<object, string> Formatter { get; set; }

        /// <summary>
        /// Gets or sets the type of the line used to display the axis.
        /// </summary>
        /// <value>The type of the line.</value>
        [Parameter]
        public LineType LineType { get; set; }

        /// <summary>
        /// Gets or sets the grid lines configuration of the current axis.
        /// </summary>
        /// <value>The grid lines.</value>
        public RadzenGridLines GridLines { get; set; } = new RadzenGridLines();

        /// <summary>
        /// Gets or sets the title configuration.
        /// </summary>
        /// <value>The title.</value>
        public RadzenAxisTitle Title { get; set; } = new RadzenAxisTitle();

        /// <summary>
        /// Gets or sets the ticks configuration.
        /// </summary>
        /// <value>The ticks.</value>
        public RadzenTicks Ticks { get; set; } = new RadzenTicks();

        /// <summary>
        /// Gets or sets the pixel distance between axis ticks. It is used to calculate the number of visible ticks depending on the available space. Set to 100 by default;
        /// Setting <see cref="Step" /> will override this value.
        /// </summary>
        /// <value>The desired pixel distance between ticks.</value>
        [Parameter]
        public int TickDistance { get; set; } = 100;

        /// <summary>
        /// Specifies the minimum value of the axis.
        /// </summary>
        /// <value>The minimum.</value>
        [Parameter]
        public object Min { get; set; }

        /// <summary>
        /// Specifies the maximum value of the axis.
        /// </summary>
        /// <value>The maximum.</value>
        [Parameter]
        public object Max { get; set; }

        /// <summary>
        /// Specifies the step of the axis.
        /// </summary>
        [Parameter]
        public object Step { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="AxisBase"/> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Visible { get; set; } = true;

        /// <inheritdoc />
        protected override bool ShouldRefreshChart(ParameterView parameters)
        {
            return DidParameterChange(parameters, nameof(Min), Min) ||
                   DidParameterChange(parameters, nameof(Max), Max) ||
                   DidParameterChange(parameters, nameof(Visible), Visible) ||
                   DidParameterChange(parameters, nameof(Step), Step);
        }

        internal string Format(ScaleBase scale, double idx)
        {
            var value = scale.Value(idx);

            return Format(scale, value);
        }

        internal string Format(ScaleBase scale, object value)
        {
            if (Formatter != null)
            {
                return Formatter(value);
            }
            else
            {
                return scale.FormatTick(FormatString, value);
            }
        }

        internal abstract double Size { get; }
    }
}
