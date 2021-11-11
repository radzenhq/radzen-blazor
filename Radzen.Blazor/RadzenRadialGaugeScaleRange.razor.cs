using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenRadialGaugeScaleRange component.
    /// </summary>
    public partial class RadzenRadialGaugeScaleRange : ComponentBase
    {
        /// <summary>
        /// Gets or sets from.
        /// </summary>
        /// <value>From.</value>
        [Parameter]
        public double From { get; set; }

        /// <summary>
        /// Gets or sets to position.
        /// </summary>
        /// <value>To.</value>
        [Parameter]
        public double To { get; set; }

        /// <summary>
        /// Gets or sets the fill.
        /// </summary>
        /// <value>The fill.</value>
        [Parameter]
        public string Fill { get; set; }

        /// <summary>
        /// Gets or sets the stroke.
        /// </summary>
        /// <value>The stroke.</value>
        [Parameter]
        public string Stroke { get; set; }

        /// <summary>
        /// Gets or sets the width of the stroke.
        /// </summary>
        /// <value>The width of the stroke.</value>
        [Parameter]
        public double StrokeWidth { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        [Parameter]
        public double Height { get; set; } = 5;

        /// <summary>
        /// Gets or sets the scale.
        /// </summary>
        /// <value>The scale.</value>
        [CascadingParameter]
        public RadzenRadialGaugeScale Scale
        {
            get; set;
        }
    }
}
