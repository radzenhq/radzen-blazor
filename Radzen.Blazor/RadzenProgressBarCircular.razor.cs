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
                "rz-progressbarcircular"
            };

            switch (Mode)
            {
                case ProgressBarMode.Determinate:
                    classList.Add("rz-progressbarcircular-determinate");
                    break;
                case ProgressBarMode.Indeterminate:
                    classList.Add("rz-progressbarcircular-indeterminate");
                    break;
            }

            classList.Add($"rz-progressbarcircular-{ProgressBarStyle.ToString().ToLowerInvariant()}");
            classList.Add($"rz-progressbarcircular-{GetCircleSize()}");

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
