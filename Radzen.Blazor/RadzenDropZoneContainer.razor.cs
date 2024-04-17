using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenDropZoneContainer component.
    /// </summary>
#if NET6_0_OR_GREATER
    [CascadingTypeParameter(nameof(TItem))]
#endif
    public partial class RadzenDropZoneContainer<TItem> : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        [Parameter]
        public IEnumerable<TItem> Data { get; set; }

        /// <summary>
        /// Gets or sets the selector function for zone items.
        /// </summary>
        /// <value>The selector function for zone items.</value>
        [Parameter]
        public Func<TItem, RadzenDropZone<TItem>, bool> ItemSelector { get; set; }

        /// <summary>
        /// Gets or sets the function that checks if the item can be dropped in specific zone or item.
        /// </summary>
        /// <value>The function that checks if the item can be dropped in specific zone.</value>
        [Parameter]
        public Func<RadzenDropZoneItemEventArgs<TItem>, bool> CanDrop { get; set; }

        /// <summary>
        /// Gets or sets the row render callback. Use it to set row attributes.
        /// </summary>
        /// <value>The row render callback.</value>
        [Parameter]
        public Action<RadzenDropZoneItemRenderEventArgs<TItem>> ItemRender { get; set; }

        /// <summary>
        /// Gets or sets the template for zone items.
        /// </summary>
        /// <value>The template for zone items.</value>
        [Parameter]
        public RenderFragment<TItem> Template { get; set; }

        /// <summary>
        /// The event callback raised on item drop.
        /// </summary>
        /// <value>The event callback raised on item drop.</value>
        [Parameter]
        public EventCallback<RadzenDropZoneItemEventArgs<TItem>> Drop { get; set; }

        internal RadzenDropZoneItemEventArgs<TItem> Payload { get; set; }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-dropzone-container";
        }
    }
}