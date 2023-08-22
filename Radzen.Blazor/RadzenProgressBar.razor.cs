using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenProgressBar component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenProgressBar @bind-Value="@value" Max="200" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenProgressBar
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var classList = new List<string>()
            {
                "rz-progressbar"
            };

            switch (Mode)
            {
                case ProgressBarMode.Determinate:
                    classList.Add("rz-progressbar-determinate");
                    break;
                case ProgressBarMode.Indeterminate:
                    classList.Add("rz-progressbar-indeterminate");
                    break;
            }
            
            classList.Add($"rz-progressbar-{ProgressBarStyle.ToString().ToLowerInvariant()}");

            return string.Join(" ", classList);
        }

        /// <summary>
        /// Gets or sets the template.
        /// </summary>
        /// <value>The  template.</value>
        [Parameter]
        public RenderFragment Template { get; set; }

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

        /// <summary>
        /// Gets or sets the progress bar style.
        /// </summary>
        /// <value>The progress bar style.</value>
        [Parameter]
        public ProgressBarStyle ProgressBarStyle { get; set; }

        /// <summary>
        /// Progress in range from 0 to 1.
        /// </summary>
        protected double NormalizedValue => Math.Min(Math.Max((Value - Min) / (Max - Min), 0), 1);
    }
}
