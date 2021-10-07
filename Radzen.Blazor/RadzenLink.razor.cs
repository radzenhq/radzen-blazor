using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public partial class RadzenLink : RadzenComponent
    {
        protected override string GetComponentCssClass()
        {
            return "rz-link";
        }

        [Parameter]
        public string Target { get; set; }

        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public string Text { get; set; } = "";

        [Parameter]
        public string Path { get; set; } = "";
    }
}