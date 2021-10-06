using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public partial class RadzenContentContainer : RadzenComponentWithChildren
    {
        [Parameter]
        public string Name { get; set; }
    }
}