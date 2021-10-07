using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public partial class RadzenLabel : RadzenComponent
    {
        [Parameter]
        public string Component { get; set; }

        [Parameter]
        public string Text { get; set; } = "";
    }
}