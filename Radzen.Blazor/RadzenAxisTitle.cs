using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Represents the title configuration of a <see cref="AxisBase" />.
    /// </summary>
    public class RadzenAxisTitle : RadzenChartComponentBase
    {
        /// <summary>
        /// Gets or sets the text displayed by the title.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public string Text { get; set; }

        /// <summary>
        /// Sets the axis with this configuration applies to.
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

        internal double Size
        {
            get
            {
                return 16 * 0.875;
            }
        }

        /// <inheritdoc />
        protected override bool ShouldRefreshChart(ParameterView parameters)
        {
            return DidParameterChange(parameters, nameof(Text), Text);
        }
    }
}