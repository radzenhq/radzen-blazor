using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenLinearGaugeScaleRange component.
    /// </summary>
    public partial class RadzenLinearGaugeScaleRange : ComponentBase
    {
        /// <summary>
        /// Gets or sets the starting value of the range.
        /// </summary>
        [Parameter]
        public double From { get; set; }

        /// <summary>
        /// Gets or sets the ending value of the range.
        /// </summary>
        [Parameter]
        public double To { get; set; }

        /// <summary>
        /// Gets or sets the range fill color.
        /// </summary>
        [Parameter]
        public string? Fill { get; set; }

        /// <summary>
        /// Gets or sets the range stroke color.
        /// </summary>
        [Parameter]
        public string? Stroke { get; set; }

        /// <summary>
        /// Gets or sets the width of the range stroke.
        /// </summary>
        [Parameter]
        public double StrokeWidth { get; set; }

        /// <summary>
        /// Gets or sets the rendered thickness of the range band.
        /// </summary>
        [Parameter]
        public double Height { get; set; } = 5;

        /// <summary>
        /// Gets or sets the corner radius of the range band in pixels. Use to produce rounded ends.
        /// </summary>
        [Parameter]
        public double BorderRadius { get; set; }

        /// <summary>
        /// Gets or sets the parent linear gauge scale.
        /// </summary>
        [CascadingParameter]
        public RadzenLinearGaugeScale? Scale { get; set; }
    }
}
