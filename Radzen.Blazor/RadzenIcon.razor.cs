using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// An icon component that displays icons from the Material Symbols font (2,500+ icons included).
    /// RadzenIcon provides a simple way to add scalable vector icons to your Blazor application without external dependencies.
    /// Uses the embedded Material Symbols Outlined variable font to render icons as text glyphs, providing benefits including no HTTP requests for icon files (icons are part of the font),
    /// vector-based icons that scale perfectly at any size, text color inheritance with coloring via IconColor or CSS, access to 2,500+ Material Symbols icons,
    /// and support for Outlined (default), Filled, Rounded, and Sharp variants via IconStyle.
    /// Icon names use underscores (e.g., "home", "account_circle", "check_circle"). See Material Symbols documentation for the full icon list.
    /// </summary>
    /// <example>
    /// Basic icon:
    /// <code>
    /// &lt;RadzenIcon Icon="home" /&gt;
    /// </code>
    /// Colored icon with custom style:
    /// <code>
    /// &lt;RadzenIcon Icon="favorite" IconColor="#FF0000" IconStyle="IconStyle.Filled" Style="font-size: 2rem;" /&gt;
    /// </code>
    /// Icons in buttons:
    /// <code>
    /// &lt;RadzenButton Icon="save" Text="Save" /&gt;
    /// &lt;RadzenButton Icon="delete" ButtonStyle="ButtonStyle.Danger" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenIcon : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the Material Symbols icon name to display.
        /// Use icon names with underscores (e.g., "home", "settings", "account_circle", "check_circle").
        /// See the Material Symbols icon library for available names.
        /// </summary>
        /// <value>The Material icon name.</value>
        [Parameter]
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets a custom color for the icon.
        /// Supports any valid CSS color value (e.g., "#FF0000", "rgb(255, 0, 0)", "var(--primary-color)").
        /// If not set, the icon inherits the current text color from its parent.
        /// </summary>
        /// <value>The icon color as a CSS color value.</value>
        [Parameter]
        public string IconColor { get; set; }

        /// <summary>
        /// Gets or sets the visual style variant of the icon.
        /// Material Symbols supports different styles: Outlined (default), Filled, Rounded, and Sharp.
        /// The style affects the icon's visual appearance (e.g., Filled icons have solid shapes vs. outlined strokes).
        /// </summary>
        /// <value>The icon style variant, or null to use the default Outlined style.</value>
        [Parameter]
        public IconStyle? IconStyle { get; set; }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"notranslate rzi{(IconStyle.HasValue ? $" rzi-{IconStyle.Value.ToString().ToLowerInvariant()}" : "")}";
        }

        string getStyle()
        {
            return $"{(!string.IsNullOrEmpty(IconColor) ? $"color:{IconColor};" : null)}{(!string.IsNullOrEmpty(Style) ? Style : null)}";
        }
    }
}
