using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenDropZone component.
    /// </summary>
    public partial class RadzenDropZone<TItem> : RadzenComponentWithChildren
    {
        /// <summary>
        /// Gets or sets the zone value used to compare items in container Selector function.
        /// </summary>
        /// <value>The zone value used to compare items in container Selector function.</value>
        [Parameter]
        public object? Value { get; set; }
        
        /// <summary>
        /// Gets or sets the Footer Templated
        /// The Footer Template is rendered below the items in the <see cref="RadzenDropZone{TItem}" />
        /// </summary>
        [Parameter]
        public RenderFragment? Footer { get; set; }

        [CascadingParameter]
        RadzenDropZoneContainer<TItem>? Container { get; set; }

        IEnumerable<TItem> Items
        {
            get
            {
                var container = Container;
                if (container?.ItemSelector != null && container.Data != null)
                {
                    return container.Data.Where(i => container.ItemSelector(i, this));
                }

                return Enumerable.Empty<TItem>();
            }
        }

        internal bool CanDrop()
        {
            var container = Container;
            if (container?.Payload != null)
            {
                container.Payload.ToZone = this;
                container.Payload.FromZone = container.Payload.FromZone;
                container.Payload.Item = container.Payload.Item;
            }

            if (container == null)
            {
                return false;
            }

            var canDrop = container.CanDrop != null && container.Payload != null ? container.CanDrop(container.Payload) : true;

            return canDrop;
        }

        internal void OnDragOver(DragEventArgs args)
        {
            var canDrop = CanDrop();
            args.DataTransfer.DropEffect = canDrop ? "move" : "none";
            cssClass = canDrop ? "rz-can-drop" : "rz-no-drop";
        }

        void OnDragLeave(DragEventArgs args)
        {
            cssClass = "";
        }

        async Task OnDrop(DragEventArgs args)
        {
            cssClass = "";
            await OnDropInternal();
        }

        internal async Task OnDropInternal()
        {
            if (CanDrop())
            {
                var container = Container;
                if (container?.Payload != null)
                {
                    await container.Drop.InvokeAsync(container.Payload);
                }
            }
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (Visible && JSRuntime != null)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.prepareDrag", Element);
            }
        }

        string cssClass = string.Empty;

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"rz-dropzone {cssClass}".Trim();
        }

        Tuple<RadzenDropZoneItemRenderEventArgs<TItem>, IReadOnlyDictionary<string, object>> ItemAttributes(TItem item)
        {
            var args = new RadzenDropZoneItemRenderEventArgs<TItem>()
            {
                Zone = this,
                Item = item
            };

            var container = Container;

            if (container?.ItemRender != null)
            {
                container.ItemRender(args);
            }

            return new Tuple<RadzenDropZoneItemRenderEventArgs<TItem>, IReadOnlyDictionary<string, object>>(args, new ReadOnlyDictionary<string, object>(args.Attributes));
        }
    }
}
