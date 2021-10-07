using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Radzen.Blazor
{
    public partial class RadzenProfileMenuItem : RadzenComponent
    {
        protected override string GetComponentCssClass()
        {
            return "rz-navigation-item";
        }

        [Parameter]
        public string Target { get; set; }

        [Parameter]
        public string Path { get; set; }

        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public string Text { get; set; }

        [Parameter]
        public string Value { get; set; }

        [CascadingParameter]
        public RadzenProfileMenu Menu { get; set; }


        public async System.Threading.Tasks.Task OnClick(MouseEventArgs args)
        {
            if (Menu != null)
            {
                Menu.Close();
                await Menu.Click.InvokeAsync(this);
            }
        }
    }
}