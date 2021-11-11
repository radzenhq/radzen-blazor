using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenContentContainer component.
    /// </summary>
    public partial class RadzenContentContainer : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [Parameter]
        public string Name { get; set; }
    }
}
