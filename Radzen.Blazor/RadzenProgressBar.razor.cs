using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// A linear progress bar component for indicating task completion or ongoing processes.
    /// RadzenProgressBar displays progress horizontally with determinate (specific value) or indeterminate (ongoing) modes.
    /// Provides visual feedback about the status of lengthy operations like file uploads, data processing, or multi-step workflows.
    /// Supports determinate mode showing specific progress value (0-100%) with a filling bar, indeterminate mode with animated bar indicating ongoing operation without specific progress,
    /// optional percentage or custom unit value display overlay, configurable Min/Max values for non-percentage scales, various semantic colors (Primary, Success, Info, Warning, Danger),
    /// custom template to override default value display, and ARIA attributes for screen reader support.
    /// Use determinate mode when you can calculate progress percentage (e.g., file upload, form completion). Use indeterminate mode for operations with unknown duration (e.g., waiting for server response).
    /// </summary>
    /// <example>
    /// Determinate progress bar:
    /// <code>
    /// &lt;RadzenProgressBar Value=@progress Max="100" ShowValue="true" ProgressBarStyle="ProgressBarStyle.Primary" /&gt;
    /// @code {
    ///     double progress = 45; // 45%
    /// }
    /// </code>
    /// Indeterminate progress bar for loading:
    /// <code>
    /// @if (isLoading)
    /// {
    ///     &lt;RadzenProgressBar Mode="ProgressBarMode.Indeterminate" ProgressBarStyle="ProgressBarStyle.Info" /&gt;
    /// }
    /// </code>
    /// Custom range and unit:
    /// <code>
    /// &lt;RadzenProgressBar Value=@bytesDownloaded Min="0" Max=@totalBytes Unit="MB" ShowValue="true" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenProgressBar
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass() => ClassList.Create("rz-progressbar")
                                                                      .Add("rz-progressbar-determinate", Mode == ProgressBarMode.Determinate)
                                                                      .Add("rz-progressbar-indeterminate", Mode == ProgressBarMode.Indeterminate)
                                                                      .Add($"rz-progressbar-{ProgressBarStyle.ToString().ToLowerInvariant()}")
                                                                      .ToString();
        /// <summary>
        /// Gets or sets a custom template for rendering content overlaid on the progress bar.
        /// Use this to display custom progress information instead of the default value/percentage display.
        /// </summary>
        /// <value>The custom content template render fragment.</value>
        [Parameter]
        public RenderFragment Template { get; set; }

        /// <summary>
        /// Gets or sets the progress bar mode determining the visual behavior.
        /// Determinate shows specific progress, Indeterminate shows continuous animation for unknown duration.
        /// </summary>
        /// <value>The progress mode. Default is <see cref="ProgressBarMode.Determinate"/>.</value>
        [Parameter]
        public ProgressBarMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the unit text displayed after the value (e.g., "%", "MB", "items").
        /// Only shown when <see cref="ShowValue"/> is true.
        /// </summary>
        /// <value>The unit text. Default is "%".</value>
        [Parameter]
        public string Unit { get; set; } = "%";

        /// <summary>
        /// Gets or sets the current progress value.
        /// Should be between <see cref="Min"/> and <see cref="Max"/>. Values outside this range are clamped.
        /// </summary>
        /// <value>The current progress value.</value>
        [Parameter]
        public double Value { get; set; }

        /// <summary>
        /// Gets or sets the minimum value of the progress range.
        /// Use non-zero values for custom progress scales (e.g., 0-1000 for byte counts).
        /// </summary>
        /// <value>The minimum value. Default is 0.</value>
        [Parameter]
        public double Min { get; set; } = 0;

        /// <summary>
        /// Gets or sets the maximum value of the progress range representing 100% completion.
        /// </summary>
        /// <value>The maximum value. Default is 100.</value>
        [Parameter]
        public double Max { get; set; } = 100;

        /// <summary>
        /// Gets or sets whether to display the progress value as text overlay on the progress bar.
        /// When true, shows the value with the unit (e.g., "45%"). Set to false for a cleaner look.
        /// </summary>
        /// <value><c>true</c> to show the value text; <c>false</c> to hide it. Default is <c>true</c>.</value>
        [Parameter]
        public bool ShowValue { get; set; } = true;

        /// <summary>
        /// Gets or sets a callback invoked when the progress value changes.
        /// Note: This is an Action, not EventCallback. For data binding, the Value property is typically bound directly.
        /// </summary>
        /// <value>The value changed callback.</value>
        [Parameter]
        public Action<double> ValueChanged { get; set; }

        /// <summary>
        /// Gets or sets the semantic color style of the progress bar.
        /// Determines the progress bar color: Primary, Success, Info, Warning, Danger, etc.
        /// </summary>
        /// <value>The progress bar style. Default uses the theme's default progress color.</value>
        [Parameter]
        public ProgressBarStyle ProgressBarStyle { get; set; }

        /// <summary>
        /// Gets or sets the ARIA label for accessibility support.
        /// Announced by screen readers to describe the progress bar's purpose (e.g., "File upload progress").
        /// </summary>
        /// <value>The ARIA label text for screen readers.</value>
        [Parameter]
        public string AriaLabel { get; set; }

        /// <summary>
        /// Progress in range from 0 to 1.
        /// </summary>
        protected double NormalizedValue => Math.Min(Math.Max((Value - Min) / (Max - Min), 0), 1);
    }
}