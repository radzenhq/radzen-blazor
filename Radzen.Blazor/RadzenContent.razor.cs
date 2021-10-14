using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenContent component.
    /// Implements the <see cref="Radzen.RadzenComponentWithChildren" />
    /// </summary>
    /// <seealso cref="Radzen.RadzenComponentWithChildren" />
    public partial class RadzenContent : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets or sets the container.
        /// </summary>
        /// <value>The container.</value>
        [Parameter]
        public string Container { get; set; }

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return "content";
        }
    }
}
