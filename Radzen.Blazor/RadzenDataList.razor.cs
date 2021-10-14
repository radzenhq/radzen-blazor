using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenDataList component.
    /// Implements the <see cref="Radzen.PagedDataBoundComponent{TItem}" />
    /// </summary>
    /// <typeparam name="TItem">The type of the t item.</typeparam>
    /// <seealso cref="Radzen.PagedDataBoundComponent{TItem}" />
    public partial class RadzenDataList<TItem> : PagedDataBoundComponent<TItem>
    {
        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return "rz-datalist-content";
        }

        /// <summary>
        /// Gets or sets a value indicating whether to wrap items.
        /// </summary>
        /// <value><c>true</c> if wrap items; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool WrapItems { get; set; }
    }
}