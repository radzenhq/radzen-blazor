using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Radzen.Blazor
{
    /// <summary>
    /// A hyperlink component for navigation within the application or to external URLs.
    /// RadzenLink provides styled links with icon support, active state highlighting, and disabled states.
    /// Enables navigation styled according to the application theme.
    /// Supports internal navigation using Path for Blazor routing without page reloads, external links opening in same or new window via Target property,
    /// automatic highlighting when the current URL matches the link path, optional icon before text via Icon property, alternative icon using custom image via Image property,
    /// disabled state that prevents navigation and changes visual appearance, and prefix or exact matching for active state detection.
    /// For internal navigation, uses Blazor's NavigationManager for client-side routing. For external URLs, use Target="_blank" to open in a new tab.
    /// </summary>
    /// <example>
    /// Internal navigation link:
    /// <code>
    /// &lt;RadzenLink Path="/products" Text="View Products" Icon="shopping_cart" /&gt;
    /// </code>
    /// External link opening in new tab:
    /// <code>
    /// &lt;RadzenLink Path="https://radzen.com" Text="Visit Radzen" Target="_blank" Icon="open_in_new" /&gt;
    /// </code>
    /// Link with custom content:
    /// <code>
    /// &lt;RadzenLink Path="/dashboard"&gt;
    ///     &lt;RadzenIcon Icon="dashboard" /&gt; Go to Dashboard
    /// &lt;/RadzenLink&gt;
    /// </code>
    /// </example>
    public partial class RadzenLink : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the alternate text for the image when using the <see cref="Image"/> property.
        /// Provides accessibility text for screen readers when an image is used instead of an icon.
        /// </summary>
        /// <value>The image alternate text. Default is "image".</value>
        [Parameter]
        public string ImageAlternateText { get; set; } = "image";

        /// <summary>
        /// Gets or sets custom child content to render as the link content.
        /// When set, overrides the <see cref="Text"/> property for complex link content with custom markup.
        /// </summary>
        /// <value>The link content render fragment.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the target window or frame for the link navigation.
        /// Use "_blank" for new tab, "_self" for same window, or custom frame names.
        /// </summary>
        /// <value>The target window/frame. Default is null (same window).</value>
        [Parameter]
        public string Target { get; set; }

        /// <summary>
        /// Gets or sets the Material icon name to display before the link text.
        /// Use Material Symbols icon names (e.g., "home", "settings", "open_in_new").
        /// </summary>
        /// <value>The Material icon name.</value>
        [Parameter]
        public string Icon { get; set; }

        /// <summary>
        /// Gets or sets a custom color for the icon.
        /// Supports any valid CSS color value. If not set, icon inherits the link color.
        /// </summary>
        /// <value>The icon color as a CSS color value.</value>
        [Parameter]
        public string IconColor { get; set; }

        /// <summary>
        /// Gets or sets a custom image URL to display before the link text instead of an icon.
        /// Alternative to using <see cref="Icon"/> for custom graphics.
        /// </summary>
        /// <value>The image URL or path.</value>
        [Parameter]
        public string Image { get; set; }

        /// <summary>
        /// Gets or sets the link text to display.
        /// For simple text links, use this property. For complex content, use <see cref="ChildContent"/> instead.
        /// </summary>
        /// <value>The link text. Default is empty string.</value>
        [Parameter]
        public string Text { get; set; } = "";

        /// <summary>
        /// Gets or sets the URL path for navigation.
        /// Can be a relative path for internal navigation (e.g., "/products") or an absolute URL for external sites.
        /// </summary>
        /// <value>The navigation path or URL. Default is empty string.</value>
        [Parameter]
        public string Path { get; set; } = "";

        /// <summary>
        /// Gets or sets how the link's active state is determined by comparing the current URL to the link path.
        /// Prefix matches when URL starts with path, All requires exact match.
        /// </summary>
        /// <value>The navigation link match mode. Default is <see cref="NavLinkMatch.Prefix"/>.</value>
        [Parameter]
        public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;

        /// <summary>
        /// Gets or sets whether the link is disabled and cannot be clicked.
        /// When disabled, the link appears grayed out and does not navigate.
        /// </summary>
        /// <value><c>true</c> if the link is disabled; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool Disabled { get; set; }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return Disabled ? "rz-link rz-link-disabled" : "rz-link";
        }

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <returns></returns>
        protected string GetPath()
        {
            return !Disabled ? Path : null;
        }

        /// <summary>
        /// Gets the target.
        /// </summary>
        /// <returns></returns>
        protected string GetTarget()
        {
            return !Disabled ? Target : null;
        }
    }
}
