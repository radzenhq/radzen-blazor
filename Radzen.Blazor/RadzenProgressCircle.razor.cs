using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenProgressCircle component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenProgressCircle @bind-Value="@value" Max="200" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenProgressCircle : ProgressBase
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var classList=new List<string>()
            {
                "rz-progresscircle"
            };

            switch (Mode)
            {
                case ProgressBarMode.Determinate:
                    classList.Add("rz-progresscircle-determinate");
                    break;
                case ProgressBarMode.Indeterminate:
                    classList.Add("rz-progresscircle-indeterminate");
                    break;
            }

            classList.Add($"rz-progresscircle-{ProgressBarStyle.ToString().ToLowerInvariant()}");
            classList.Add($"rz-progresscircle-{getCircleSize()}");

            return string.Join(" ", classList);
        }

        private string getCircleSize()
        {
            switch (Size)
            {
                case ProgressBarCircularSize.Medium:
                    return "md";
                case ProgressBarCircularSize.Large:
                    return "lg";
                case ProgressBarCircularSize.Small:
                    return "sm";
                case ProgressBarCircularSize.ExtraSmall:
                    return "xs";
                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// Gets or sets the mode.
        /// </summary>
        /// <value>The mode.</value>
        [Parameter]
        public ProgressBarMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the progress circle style.
        /// </summary>
        /// <value>The progress circle style.</value>
        [Parameter]
        public ProgressBarStyle ProgressBarStyle { get; set; }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        [Parameter]
        public ProgressBarCircularSize Size { get; set; } = ProgressBarCircularSize.Medium;
    }
} 
