using Microsoft.AspNetCore.Components.Web;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenDataGridHeaderCell.
    /// </summary>
    public partial class RadzenDataGridHeaderCell<TItem> where TItem : notnull
    {
        bool stopKeydownPropagation = true;

        void OnGuardKeyDown(KeyboardEventArgs args)
        {
            var key = args.Code ?? args.Key;
            stopKeydownPropagation = key != "Escape";
        }
    }
}

