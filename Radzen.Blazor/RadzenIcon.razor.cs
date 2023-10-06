using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenIcon component. Displays icon from Material Icons font.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenIcon Icon="3d_rotation" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenIcon : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        /// <value>The icon.</value>
        [Parameter]
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets the icon color.
        /// </summary>
        /// <value>The icon color.</value>
        [Parameter]
        public string IconColor { get; set; }

        /// <summary>
        /// Specifies the display style of the icon.
        /// </summary>
        [Parameter]
        public IconStyle? IconStyle { get; set; }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"rzi{(IconStyle.HasValue ? $" rzi-{IconStyle.Value.ToString().ToLowerInvariant()}" : "")}";
        }

        string getStyle()
        {
            return $"{(!string.IsNullOrEmpty(IconColor) ? $"color:{IconColor};" : null)}{(!string.IsNullOrEmpty(Style) ? Style : null)}";
        }
    }
}
