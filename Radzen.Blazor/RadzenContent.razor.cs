using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public partial class RadzenContent : RadzenComponentWithChildren
    {
        [Parameter]
        public string Container { get; set; }

        protected override string GetComponentCssClass()
        {
            return "content";
        }
    }
}
