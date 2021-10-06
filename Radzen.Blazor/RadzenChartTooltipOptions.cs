using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public partial class RadzenChartTooltipOptions : RadzenChartComponentBase
    {
        [Parameter]
        public bool Visible { get; set; } = true;

        [Parameter]
        public string Style { get; set; }

        protected override void Initialize()
        {
            Chart.Tooltip = this;
        }

        protected override bool ShouldRefreshChart(ParameterView parameters)
        {
            return parameters.DidParameterChange(nameof(Style), Style);
        }
    }
}
