using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public partial class RadzenHtml : ComponentBase
    {
        [Parameter]
        public bool Visible { get; set; } = true;

        [Parameter]
        public RenderFragment ChildContent { get; set; }
    }
}