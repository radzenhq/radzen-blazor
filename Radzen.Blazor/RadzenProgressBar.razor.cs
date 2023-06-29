using Microsoft.AspNetCore.Components;
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
    public partial class RadzenProgressBar : ProgressBase
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var classList=new List<string>()
            {
                "rz-progressbar"
            };

            switch (Mode)
            {
                case ProgressBarMode.Determinate:
                    classList.Add("rz-progressbar-determinate");
                    classList.Add($"rz-progressbar-determinate-{ProgressBarStyle.ToString().ToLowerInvariant()}");
                    break;
                case ProgressBarMode.Indeterminate:
                    classList.Add("rz-progressbar-indeterminate");
                    classList.Add($"rz-progressbar-indeterminate-{ProgressBarStyle.ToString().ToLowerInvariant()}");
                    break;
            }

            return string.Join(" ", classList);
        }

        /// <summary>
        /// Gets or sets the mode.
        /// </summary>
        /// <value>The mode.</value>
        [Parameter]
        public ProgressBarMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the progress bar style.
        /// </summary>
        /// <value>The progress bar style.</value>
        [Parameter]
        public ProgressBarStyle ProgressBarStyle { get; set; }
    }
} 
