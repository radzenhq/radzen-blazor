using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Radzen.Blazor
{
    public partial class RadzenImage : RadzenComponentWithChildren
    {
        [Parameter]
        public string Path { get; set; }

        [Parameter]
        public EventCallback<MouseEventArgs> Click { get; set; }

        protected async System.Threading.Tasks.Task OnClick(MouseEventArgs args)
        {
            await Click.InvokeAsync(args);
        }
    }
}