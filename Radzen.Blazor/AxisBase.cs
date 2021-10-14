using System;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class AxisBase.
    /// Implements the <see cref="Radzen.Blazor.RadzenChartComponentBase" />
    /// Implements the <see cref="Radzen.Blazor.IChartAxis" />
    /// </summary>
    /// <seealso cref="Radzen.Blazor.RadzenChartComponentBase" />
    /// <seealso cref="Radzen.Blazor.IChartAxis" />
    public abstract class AxisBase : RadzenChartComponentBase, IChartAxis
    {
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
        /// Gets or sets the format string.
        /// </summary>
        /// <value>The format string.</value>
        [Parameter]
        public string FormatString { get; set; }

        /// <summary>
        /// Gets or sets the formatter.
        /// </summary>
        /// <value>The formatter.</value>
        [Parameter]
        public Func<object, string> Formatter { get; set; }

        /// <summary>
        /// Gets or sets the type of the line.
        /// </summary>
        /// <value>The type of the line.</value>
        [Parameter]
        public LineType LineType { get; set; }

        /// <summary>
        /// Gets or sets the grid lines.
        /// </summary>
        /// <value>The grid lines.</value>
        public RadzenGridLines GridLines { get; set; } = new RadzenGridLines();

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        public RadzenAxisTitle Title { get; set; } = new RadzenAxisTitle();

        /// <summary>
        /// Gets or sets the ticks.
        /// </summary>
        /// <value>The ticks.</value>
        public RadzenTicks Ticks { get; set; } = new RadzenTicks();

        /// <summary>
        /// Gets or sets the tick distance.
        /// </summary>
        /// <value>The tick distance.</value>
        internal int TickDistance { get; set; } = 100;

        /// <summary>
        /// Determines the minimum of the parameters.
        /// </summary>
        /// <value>The minimum.</value>
        [Parameter]
        public object Min { get; set; }

        /// <summary>
        /// Determines the maximum of the parameters.
        /// </summary>
        /// <value>The maximum.</value>
        [Parameter]
        public object Max { get; set; }

        /// <summary>
        /// Gets or sets the step.
        /// </summary>
        /// <value>The step.</value>
        [Parameter]
        public object Step { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="AxisBase"/> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Shoulds the refresh chart.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool ShouldRefreshChart(ParameterView parameters)
        {
            return DidParameterChange(parameters, nameof(Min), Min) ||
                   DidParameterChange(parameters, nameof(Max), Max) ||
                   DidParameterChange(parameters, nameof(Visible), Visible) ||
                   DidParameterChange(parameters, nameof(Step), Step);
        }

        /// <summary>
        /// Formats the specified scale.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <param name="idx">The index.</param>
        /// <returns>System.String.</returns>
        internal string Format(ScaleBase scale, double idx)
        {
            var value = scale.Value(idx);

            return Format(scale, value);
        }

        /// <summary>
        /// Formats the specified scale.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <param name="value">The value.</param>
        /// <returns>System.String.</returns>
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

        /// <summary>
        /// Gets the size.
        /// </summary>
        /// <value>The size.</value>
        internal abstract double Size { get; }
    }
}
