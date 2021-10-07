using Microsoft.AspNetCore.Components;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    public partial class RadzenRadialGaugeScalePointer : ComponentBase
    {
        [Parameter]
        public string Fill { get; set; }

        [Parameter]
        public double Value { get; set; }

        [Parameter]
        public bool ShowValue { get; set; } = true;

        [Parameter]
        public double Radius { get; set; } = 10;

        [Parameter]
        public double? Width { get; set; }

        double CurrentWidth
        {
            get
            {
                return Width ?? Radius / 2;
            }
        }

        [Parameter]
        public double Length { get; set; } = 1;

        [Parameter]
        public string FormatString { get; set; }

        double CurrentLength
        {
            get
            {
                return Scale.CurrentRadius * Length;
            }
        }

        [Parameter]
        public string Stroke { get; set; }

        [Parameter]
        public double StrokeWidth { get; set; }

        [CascadingParameter]
        public RadzenRadialGaugeScale Scale { get; set; }

        [Parameter]
        public RenderFragment<RadzenRadialGaugeScalePointer> Template { get; set; }

        [CascadingParameter]
        public RadzenRadialGauge Gauge { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            Gauge.AddPointer(this);
        }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var shouldRefresh = false;

            if (parameters.DidParameterChange(nameof(Value), Value) || parameters.DidParameterChange(nameof(ShowValue), ShowValue))
            {
                shouldRefresh = true;
            }

            await base.SetParametersAsync(parameters);

            if (shouldRefresh)
            {
                Gauge.Reload();
            }
        }
    }
}