using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenHtml.
    /// Implements the <see cref="ComponentBase" />
    /// </summary>
    /// <seealso cref="ComponentBase" />
    public partial class RadzenHtml : ComponentBase
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenHtml"/> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool Visible { get; set; } = true;

        /// <summary>
        /// Gets or sets the content of the child.
        /// </summary>
        /// <value>The content of the child.</value>
        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}