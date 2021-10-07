using Microsoft.AspNetCore.Components;
using System;

namespace Radzen.Blazor
{
    public partial class RadzenSidebarToggle : RadzenComponent
    {
        [Parameter]
        public EventCallback<EventArgs> Click { get; set; }

        public async System.Threading.Tasks.Task OnClick(EventArgs args)
        {
            await Click.InvokeAsync(args);
        }

        protected override string GetComponentCssClass()
        {
            return "sidebar-toggle";
        }
    }
}