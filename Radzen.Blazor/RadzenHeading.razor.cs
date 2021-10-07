using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public partial class RadzenHeading : RadzenComponent
    {
        [Parameter]
        public string Text { get; set; }

        [Parameter]
        public string Size { get; set; } = "H1";

        protected override string GetComponentCssClass()
        {
            return "rz-heading";
        }
    }
}