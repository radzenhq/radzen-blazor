using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenProgressBarCircular component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenProgressBarCircular @bind-Value="@value" Max="200" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenProgressBarCircular : RadzenProgressBar
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            var classList=new List<string>()
            {
                "rz-progressbar-circular"
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
            classList.Add($"rz-progressbar-circular-{GetCircleSize()}");

            return string.Join(" ", classList);
        }

        /// <summary>
        /// Gets the circle size.
        /// </summary>
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
