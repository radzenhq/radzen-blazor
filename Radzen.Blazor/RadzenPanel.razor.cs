using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    public partial class RadzenPanel : RadzenComponentWithChildren
    {
        protected override string GetComponentCssClass()
        {
            return "rz-panel";
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
        public RenderFragment SummaryTemplate { get; set; } = null;

        [Parameter]
        public RenderFragment FooterTemplate { get; set; }

        [Parameter]
        public EventCallback Expand { get; set; }

        [Parameter]
        public EventCallback Collapse { get; set; }

        string contentStyle = "display: block;";
        string summaryContentStyle = "display: none";

        async System.Threading.Tasks.Task Toggle(MouseEventArgs args)
        {
            collapsed = !collapsed;
            contentStyle = collapsed ? "display: none;" : "display: block;";
            summaryContentStyle = !collapsed ? "display: none" : "display: block";

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
            contentStyle = collapsed ? "display: none;" : "display: block;";
            summaryContentStyle = !collapsed ? "display: none" : "display: block";

            return base.OnParametersSetAsync();
        }
    }
}