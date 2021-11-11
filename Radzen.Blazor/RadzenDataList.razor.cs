using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenDataList component.
    /// </summary>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenDataList @data=@orders TItem="Order" AllowPaging="true" WrapItems="true"&gt;
    ///     &lt;Template&gt;
    ///         @context.OrderId
    ///     &lt;/Template&gt;
    /// &lt;/RadzenDataList&gt;
    /// </code>
    /// </example>
    public partial class RadzenDataList<TItem> : PagedDataBoundComponent<TItem>
    {
        /// <inheritdoc />
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
