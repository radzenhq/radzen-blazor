using System;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public abstract class AxisBase : RadzenChartComponentBase, IChartAxis
    {
        [Parameter]
        public string Stroke { get; set; }
        [Parameter]
        public double StrokeWidth { get; set; } = 1;

        [Parameter]
        public RenderFragment ChildContent { get; set; }

        [Parameter]
        public string FormatString { get; set; }

        [Parameter]
        public Func<object, string> Formatter { get; set; }

        [Parameter]
        public LineType LineType { get; set; }

        public RadzenGridLines GridLines { get; set; } = new RadzenGridLines();

        public RadzenAxisTitle Title { get; set; } = new RadzenAxisTitle();

        public RadzenTicks Ticks { get; set; } = new RadzenTicks();

        internal int TickDistance { get; set; } = 100;

        [Parameter]
        public object Min { get; set; }

        [Parameter]
        public object Max { get; set; }

        [Parameter]
        public object Step { get; set; }

        [Parameter]
        public bool Visible { get; set; } = true;

        protected override bool ShouldRefreshChart(ParameterView parameters)
        {
            return DidParameterChange(parameters, nameof(Min), Min) ||
                   DidParameterChange(parameters, nameof(Max), Max) ||
                   DidParameterChange(parameters, nameof(Visible), Visible) ||
                   DidParameterChange(parameters, nameof(Step), Step);
        }

        internal string Format(ScaleBase scale, double idx)
        {
            var value = scale.Value(idx);

            return Format(scale, value);
        }

        internal string Format(ScaleBase scale, object value)
        {
            if (Formatter != null)
            {
                return Formatter(value);
            }
            else
            {
                return scale.FormatTick(FormatString, value);
            }
        }

        internal abstract double Size { get; }
    }
}
