using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;

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
        protected override string GetComponentCssClass() => ClassList.Create("rz-progressbar-circular")
                                                                     .Add("rz-progressbar-determinate", Mode == ProgressBarMode.Determinate)
                                                                     .Add("rz-progressbar-indeterminate", Mode == ProgressBarMode.Indeterminate)
                                                                     .Add($"rz-progressbar-{ProgressBarStyle.ToString().ToLowerInvariant()}")
                                                                     .Add($"rz-progressbar-circular-{CircleSize}")
                                                                     .ToString();

        string CircleSize => Size switch
        {
            ProgressBarCircularSize.Medium => "md",
            ProgressBarCircularSize.Large => "lg",
            ProgressBarCircularSize.Small => "sm",
            ProgressBarCircularSize.ExtraSmall => "xs",
            _ => string.Empty,
        };

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        [Parameter]
        public ProgressBarCircularSize Size { get; set; } = ProgressBarCircularSize.Medium;
    }
} 