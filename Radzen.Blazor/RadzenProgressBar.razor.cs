using Microsoft.AspNetCore.Components;
using System;

namespace Radzen.Blazor
{
    public partial class RadzenProgressBar : RadzenComponent
    {
        protected override string GetComponentCssClass()
        {
            return Mode == ProgressBarMode.Determinate ? "rz-progressbar rz-progressbar-determinate" : "rz-progressbar rz-progressbar-indeterminate";
        }

        [Parameter]
        public ProgressBarMode Mode { get; set; }

        [Parameter]
        public string Unit { get; set; } = "%";

        [Parameter]
        public double Value { get; set; }

        [Parameter]
        public double Max { get; set; } = 100;

        [Parameter]
        public bool ShowValue { get; set; } = true;

        [Parameter]
        public Action<double> ValueChanged { get; set; }
    }
}