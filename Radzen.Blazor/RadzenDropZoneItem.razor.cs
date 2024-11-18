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

        void EnsurePayload(DragEventArgs args = null)
        {
            Container.Payload = new RadzenDropZoneItemEventArgs<TItem>()
            {
                FromZone = Zone,
                Item = Item,
                DataTransfer = args?.DataTransfer
            };
        }

        void OnDragStart()
        {
            dragCssClass = "rz-dragging";
            EnsurePayload();
        }

        void OnDragOver(DragEventArgs args)
        {
            if (Container.Payload == null)
            {
                EnsurePayload(args);
            }

            Container.Payload.ToItem = Item;

            var canDrop = Zone.CanDrop();
            args.DataTransfer.DropEffect = canDrop ? "move" : "none";
            cssClass = canDrop ? "rz-can-drop" : "rz-no-drop";

            Zone.OnDragOver(args);
        }

        void OnDragLeave(DragEventArgs args)
        {
            cssClass = "";
        }

        void OnDragEnd(DragEventArgs args)
        {
            dragCssClass = "";
        }

        async Task OnDrop(DragEventArgs args)
        {
            if (Container.Payload == null)
            {
                EnsurePayload(args);
            }
            cssClass = "";
            Container.Payload.ToItem = Item;
            await Zone.OnDropInternal();
        }

        string cssClass;
        string dragCssClass;

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"rz-dropzone-item {cssClass} {dragCssClass}".Trim();
        }
    }
}
