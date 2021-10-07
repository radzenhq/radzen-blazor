using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public partial class RadzenRadialGaugeScaleRange : ComponentBase
    {
        [Parameter]
        public double From { get; set; }

        [Parameter]
        public double To { get; set; }

        [Parameter]
        public string Fill { get; set; }

        [Parameter]
        public string Stroke { get; set; }

        [Parameter]
        public double StrokeWidth { get; set; }

        [Parameter]
        public double Height { get; set; } = 5;

        [CascadingParameter]
        public RadzenRadialGaugeScale Scale
        {
            get; set;
        }
    }
}