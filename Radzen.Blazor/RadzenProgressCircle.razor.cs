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
                case ProgressCircleMode.Determinate:
                    classList.Add("rz-progresscircle-determinate");
                    break;
                case ProgressCircleMode.Indeterminate:
                    classList.Add("rz-progresscircle-indeterminate");
                    break;
            }

            classList.Add($"rz-progresscircle-{ProgressCircleStyle.ToString().ToLowerInvariant()}");
            classList.Add($"rz-progresscircle-{getCircleSize()}");

            return string.Join(" ", classList);
        }

        private string getCircleSize()
        {
            switch (Size)
            {
                case ProgressCircleSize.Medium:
                    return "md";
                case ProgressCircleSize.Large:
                    return "lg";
                case ProgressCircleSize.Small:
                    return "sm";
                case ProgressCircleSize.ExtraSmall:
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
        public ProgressCircleMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the progress circle style.
        /// </summary>
        /// <value>The progress circle style.</value>
        [Parameter]
        public ProgressCircleStyle ProgressCircleStyle { get; set; }

        /// <summary>
        /// Gets or sets the mode.
        /// </summary>
        /// <value>The mode.</value>
        [Parameter]
        public ProgressCircleSize Size { get; set; } = ProgressCircleSize.Medium;
    }
} 
