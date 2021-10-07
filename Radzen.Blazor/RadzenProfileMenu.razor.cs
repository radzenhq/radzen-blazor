using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Radzen.Blazor
{
    public partial class RadzenProfileMenu : RadzenComponentWithChildren
    {
        protected override string GetComponentCssClass()
        {
            return "rz-menu rz-profile-menu";
        }

        [Parameter]
        public RenderFragment Template { get; set; }

        [Parameter]
        public EventCallback<RadzenProfileMenuItem> Click { get; set; }

        string contentStyle = "display:none;position:absolute;z-index:1;";
        string iconStyle = "transform: rotate(0deg);";

        public void Toggle(MouseEventArgs args)
        {
            contentStyle = contentStyle.IndexOf("display:none;") != -1 ? "display:block;" : "display:none;position:absolute;z-index:1;";
            iconStyle = iconStyle.IndexOf("rotate(0deg)") != -1 ? "transform: rotate(-180deg);" : "transform: rotate(0deg);";
            StateHasChanged();
        }

        public void Close()
        {
            contentStyle = "display:none;";
            iconStyle = "transform: rotate(0deg);";
            StateHasChanged();
        }
    }
}