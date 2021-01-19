using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public abstract class RadzenChartComponentBase : ComponentBase
    {
        private RadzenChart chart;

        [CascadingParameter]
        public RadzenChart Chart
        {
            get
            {
                return chart;
            }
            set
            {
                chart = value;
                Initialize();
            }
        }

        protected virtual void Initialize()
        {

        }

        protected virtual bool ShouldRefreshChart(ParameterView parameters)
        {
            return false;
        }

        public override async Task SetParametersAsync(ParameterView parameters)
        {
            bool shouldRefresh = ShouldRefreshChart(parameters);

            await base.SetParametersAsync(parameters);

            ValidateParameters();

            if (shouldRefresh)
            {
                Chart.Refresh();
            }
        }

        protected virtual void ValidateParameters()
        {
        }

        protected bool DidParameterChange<T>(ParameterView parameters, string parameterName, T parameterValue)
        {
            return parameters.DidParameterChange(parameterName, parameterValue);
        }
    }
}
