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
    public partial class RadzenProgressCircle : RadzenProgressBar
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
            classList.Add($"rz-progresscircle-{GetCircleSize()}");

            return string.Join(" ", classList);
        }

        protected string GetCircleSize()
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
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        [Parameter]
        public ProgressBarCircularSize Size { get; set; } = ProgressBarCircularSize.Medium;
    }
} 
