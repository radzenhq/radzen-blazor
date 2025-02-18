using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenLink component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenLink Path="https://www.radzen.com" Text="Go to url" Target="_blank" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenLink : RadzenComponent
    {
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public string ImageAlternateText { get; set; } = "image";

        /// <summary>
        /// Gets or sets the child content.
        /// </summary>
        /// <value>The child content.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Gets or sets the target.
        /// </summary>
        /// <value>The target.</value>
        [Parameter]
        public string Target { get; set; }

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
        /// Gets or sets the image.
        /// </summary>
        /// <value>The image.</value>
        [Parameter]
        public string Image { get; set; }

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public string Text { get; set; } = "";

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        [Parameter]
        public string Path { get; set; } = "";

        /// <summary>
        /// Gets or sets the navigation link match.
        /// </summary>
        /// <value>The navigation link match.</value>
        [Parameter]
        public NavLinkMatch Match { get; set; } = NavLinkMatch.Prefix;

        /// <summary>
        /// Gets or sets whether the link is disabled.
        /// </summary>
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
