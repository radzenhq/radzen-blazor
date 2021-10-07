using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Radzen.Blazor
{
    public partial class RadzenMenuItem : RadzenComponent
    {
        protected override string GetComponentCssClass()
        {
            return "rz-navigation-item";
        }

        [Parameter]
        public string Target { get; set; }

        [Parameter]
        public string Text { get; set; }

        [Parameter]
        public object Value { get; set; }

        [Parameter]
        public string Path { get; set; }

        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public string Image { get; set; }

        [Parameter]
        public RenderFragment Template { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [CascadingParameter]
        public RadzenMenu Parent { get; set; }

        public async System.Threading.Tasks.Task OnClick(MouseEventArgs args)
        {
            if (Parent != null)
            {
                await Parent.Click.InvokeAsync(new MenuItemEventArgs() { Text = this.Text, Path = this.Path, Value = this.Value });
            }
        }
    }
}