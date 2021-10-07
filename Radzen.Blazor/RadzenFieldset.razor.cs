using Microsoft.AspNetCore.Components;
using System;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    public partial class RadzenFieldset : RadzenComponent
    {
        protected override string GetComponentCssClass()
        {
            return AllowCollapse ? "rz-fieldset rz-fieldset-toggleable" : "rz-fieldset";
        }

        [Parameter]
        public bool AllowCollapse { get; set; }

        private bool collapsed;
        [Parameter]
        public bool Collapsed { get; set; }

        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public string Text { get; set; } = "";

        [Parameter]
        public RenderFragment HeaderTemplate { get; set; }

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public RenderFragment SummaryTemplate { get; set; } = null;

        [Parameter]
        public EventCallback Expand { get; set; }

        [Parameter]
        public EventCallback Collapse { get; set; }

        string contentStyle = "";
        string summaryContentStyle = "display: none";

        async System.Threading.Tasks.Task Toggle(EventArgs args)
        {
            collapsed = !collapsed;
            contentStyle = collapsed ? "display: none;" : "";
            summaryContentStyle = !collapsed ? "display: none" : "";

            if (collapsed)
            {
                await Collapse.InvokeAsync(args);
            }
            else
            {
                await Expand.InvokeAsync(args);
            }

            StateHasChanged();
        }

        protected override void OnInitialized()
        {
            collapsed = Collapsed;
        }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Collapsed), Collapsed))
            {
                collapsed = parameters.GetValueOrDefault<bool>(nameof(Collapsed));
            }

            await base.SetParametersAsync(parameters);
        }

        protected override Task OnParametersSetAsync()
        {
            contentStyle = collapsed ? "display: none;" : "";
            summaryContentStyle = !collapsed ? "display: none" : "";

            return base.OnParametersSetAsync();
        }
    }
}