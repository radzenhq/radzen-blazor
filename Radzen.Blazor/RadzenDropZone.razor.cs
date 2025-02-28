using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web.Virtualization;

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
        public object Value { get; set; }
        
        /// <summary>
        /// Gets or sets the Footer Templated
        /// The Footer Template is rendered below the items in the <see cref="RadzenDropZone{TItem}" />
        /// </summary>
        [Parameter]
        public RenderFragment Footer { get; set; }

        [CascadingParameter]
        RadzenDropZoneContainer<TItem> Container { get; set; }

        [Parameter]
        public bool AllowVirtualization { get; set; }

        internal Virtualize<TItem> virtualize = default!;

        IEnumerable<TItem> Items
        {
            get
            {
                return Container.ItemSelector != null ? Container.Data.Where(i => Container.ItemSelector(i, this)) : Enumerable.Empty<TItem>();
            }
        }
        private async ValueTask<ItemsProviderResult<TItem>> LoadItems(ItemsProviderRequest request)
        {
            var top = request.Count;

            var totalItemsCount = Items.Count(); 

            var virtualDataItems = Items.Skip(request.StartIndex).Take(top);
            
            return new ItemsProviderResult<TItem>(virtualDataItems, totalItemsCount);
        }
        RenderFragment DrawDropZoneItems()
        {
            return new RenderFragment(builder =>
            {
                if (AllowVirtualization)
                {
                    builder.OpenComponent(0, typeof(Virtualize<TItem>));
                    builder.AddAttribute(1, "ItemsProvider", new ItemsProviderDelegate<TItem>(LoadItems));
                    
                    builder.AddAttribute(2, "ChildContent", (RenderFragment<TItem>)((context) =>
                    {
                        return (RenderFragment)((b) =>
                        {
                            DrawItem(b, context);
                        });
                    }));

                    builder.AddComponentReferenceCapture(4, c => { virtualize = (Virtualize<TItem>)c; });

                    builder.CloseComponent();
                }
                else
                {
                    DrawItems(builder);
                }
            });
        }  

        internal void DrawItems(RenderTreeBuilder builder)
        {
            
            foreach (var item in Items)
            {
               
                DrawItem(builder, item);
            }
        }    

        internal void DrawItem(RenderTreeBuilder builder, TItem item)
        {
            var result = ItemAttributes(item);

            builder.OpenComponent<CascadingValue<TItem>>(0);
            builder.AddAttribute(1, "Value", item);
            builder.AddAttribute(2, "ChildContent", (RenderFragment)((builder2) => {
                        builder2.OpenComponent<CascadingValue<RadzenDropZone<TItem>>>(3);
                        builder2.AddAttribute(4, "Value", this);
                        builder2.AddAttribute(5, "ChildContent", (RenderFragment)((builder3) => {
                                    builder3.OpenComponent<RadzenDropZoneItem<TItem>>(6);
                                    builder3.AddAttribute(7, "Visible", result.Item1.Visible);
                                    builder3.AddAttribute(8, "Attributes", result.Item2);
                                    builder3.CloseComponent();
                                }));
                        builder2.CloseComponent();
                    }));
            builder.CloseComponent();
        }

        internal bool CanDrop()
        {
            if (Container.Payload != null)
            {
                Container.Payload.ToZone = this;
                Container.Payload.FromZone = Container.Payload.FromZone;
                Container.Payload.Item = Container.Payload.Item;
            }

            var canDrop = Container.CanDrop != null && Container.Payload != null ? Container.CanDrop(Container.Payload) : true;

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
                await Container.Drop.InvokeAsync(Container.Payload);
            }
        }

        /// <inheritdoc />
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (Visible)
            {
                await JSRuntime.InvokeVoidAsync("Radzen.prepareDrag", Element);
            }
        }

        string cssClass;

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

            if (Container.ItemRender != null)
            {
                Container.ItemRender(args);
            }

            return new Tuple<RadzenDropZoneItemRenderEventArgs<TItem>, IReadOnlyDictionary<string, object>>(args, new ReadOnlyDictionary<string, object>(args.Attributes));
        }
    }
}
