using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenAxisTitle.
    /// Implements the <see cref="Radzen.Blazor.RadzenChartComponentBase" />
    /// </summary>
    /// <seealso cref="Radzen.Blazor.RadzenChartComponentBase" />
    public class RadzenAxisTitle : RadzenChartComponentBase
    {
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public string Text { get; set; }

        /// <summary>
        /// Sets the axis.
        /// </summary>
        /// <value>The axis.</value>
        [CascadingParameter]
        public AxisBase Axis
        {
            set
            {
                value.Title = this;
            }
        }

        /// <summary>
        /// Gets the size.
        /// </summary>
        /// <value>The size.</value>
        internal double Size
        {
            get
            {
                return 16 * 0.875;
            }
        }

        /// <summary>
        /// Shoulds the refresh chart.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool ShouldRefreshChart(ParameterView parameters)
        {
            return DidParameterChange(parameters, nameof(Text), Text);
        }
    }
}