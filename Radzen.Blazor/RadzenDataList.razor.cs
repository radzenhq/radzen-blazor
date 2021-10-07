using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public partial class RadzenDataList<TItem> : PagedDataBoundComponent<TItem>
    {
        protected override string GetComponentCssClass()
        {
            return "rz-datalist-content";
        }

        [Parameter]
        public bool WrapItems { get; set; }
    }
}