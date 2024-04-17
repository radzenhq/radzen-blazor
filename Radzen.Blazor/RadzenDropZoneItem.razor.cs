using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenDropZoneItem component.
    /// </summary>
    public partial class RadzenDropZoneItem<TItem> : RadzenComponent
    {
        [CascadingParameter]
        TItem Item { get; set; }

        [CascadingParameter]
        RadzenDropZone<TItem> Zone { get; set; }

        [CascadingParameter]
        RadzenDropZoneContainer<TItem> Container { get; set; }

        void OnDragStart()
        {
            Container.Payload = new RadzenDropZoneItemEventArgs<TItem>()
            {
                FromZone = Zone,
                Item = Item
            };
        }

        void OnDragEnter(DragEventArgs args)
        {
            Container.Payload.ToItem = Item;
            Zone.OnDragEnter(args);
        }

        async Task OnDrop(DragEventArgs args)
        {
            Container.Payload.ToItem = Item;
            await Zone.OnDropInternal();
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return "rz-dropzone-item";
        }
    }
}
