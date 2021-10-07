using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public partial class RadzenIcon : RadzenComponent
    {
        [Parameter]
        public string Icon { get; set; }

        protected override string GetComponentCssClass()
        {
            return "rzi d-inline-flex justify-content-center align-items-center";
        }
    }
}