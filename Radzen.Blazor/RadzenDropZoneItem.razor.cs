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
        TItem Item { get; set; } = default!;

        [CascadingParameter]
        RadzenDropZone<TItem> Zone { get; set; } = default!;

        [CascadingParameter]
        RadzenDropZoneContainer<TItem> Container { get; set; } = default!;

        void EnsurePayload(DragEventArgs? args = null)
        {
            Container.Payload = new RadzenDropZoneItemEventArgs<TItem>()
            {
                FromZone = Zone,
                Item = Item,
                DataTransfer = args?.DataTransfer ?? default!
            };
        }

        void OnDragStart()
        {
            dragCssClass = "rz-dragging";
            EnsurePayload();
        }

        void OnDragOver(DragEventArgs args)
        {
            if (Container?.Payload == null)
            {
                EnsurePayload(args);
            }

            if (Container?.Payload != null)
            {
                Container.Payload.ToItem = Item;
            }

            var canDrop = Zone?.CanDrop() ?? false;
            args.DataTransfer.DropEffect = canDrop ? "move" : "none";
            cssClass = canDrop ? "rz-can-drop" : "rz-no-drop";

            Zone?.OnDragOver(args);
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
            if (Container?.Payload == null)
            {
                EnsurePayload(args);
            }
            cssClass = "";
            if (Container?.Payload != null)
            {
                Container.Payload.ToItem = Item;
            }
            if (Zone != null)
            {
                await Zone.OnDropInternal();
            }
        }

        string cssClass = string.Empty;
        string dragCssClass = string.Empty;

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return $"rz-dropzone-item {cssClass} {dragCssClass}".Trim();
        }
    }
}
