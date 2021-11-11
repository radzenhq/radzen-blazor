using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenContent component.
    /// </summary>
    public partial class RadzenContent : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets or sets the container.
        /// </summary>
        /// <value>The container.</value>
        [Parameter]
        public string Container { get; set; }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "content";
        }
    }
}
