using Microsoft.AspNetCore.Components;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenProgressBar component.
    /// Implements the <see cref="Radzen.RadzenComponent" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponent" />
    public partial class RadzenProgressBar : RadzenComponent
    {
        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return Mode == ProgressBarMode.Determinate ? "rz-progressbar rz-progressbar-determinate" : "rz-progressbar rz-progressbar-indeterminate";
        }

        /// <summary>
        /// Gets or sets the mode.
        /// </summary>
        /// <value>The mode.</value>
        [Parameter]
        public ProgressBarMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the unit.
        /// </summary>
        /// <value>The unit.</value>
        [Parameter]
        public string Unit { get; set; } = "%";

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>The value.</value>
        [Parameter]
        public double Value { get; set; }

        /// <summary>
        /// Determines the maximum value.
        /// </summary>
        /// <value>The maximum value.</value>
        [Parameter]
        public double Max { get; set; } = 100;

        /// <summary>
        /// Gets or sets a value indicating whether value is shown.
        /// </summary>
        /// <value><c>true</c> if value is shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowValue { get; set; } = true;

        /// <summary>
        /// Gets or sets the value changed callback.
        /// </summary>
        /// <value>The value changed callback.</value>
        [Parameter]
        public Action<double> ValueChanged { get; set; }
    }
}