using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Base class of components that are rendered inside a <see cref="RadzenChart" />.
    /// </summary>
    public abstract class RadzenChartComponentBase : ComponentBase
    {
        private RadzenChart chart;

        /// <summary>
        /// Used to inject the RadzenChart instance. Invokes <see cref="Initialize" />.
        /// </summary>
        /// <value>The chart.</value>
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

        /// <summary>
        /// Perform initialization when <see cref="Chart" /> is set.
        /// </summary>
        protected virtual void Initialize()
        {

        }

        /// <summary>
        /// Determines if RadzenChart should render.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        protected virtual bool ShouldRefreshChart(ParameterView parameters)
        {
            return false;
        }

        /// <inheritdoc />
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            bool shouldRefresh = ShouldRefreshChart(parameters);

            await base.SetParametersAsync(parameters);

            ValidateParameters();

            if (shouldRefresh)
            {
                await Chart.Refresh();
            }
        }

        /// <summary>
        /// Validates the parameters.
        /// </summary>
        protected virtual void ValidateParameters()
        {
        }

        /// <summary>
        /// Checks if the specified parameter changed.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters">The parameters.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="parameterValue">The parameter value.</param>
        /// <returns><c>true</c> if the parameter changed, <c>false</c> otherwise.</returns>
        protected bool DidParameterChange<T>(ParameterView parameters, string parameterName, T parameterValue)
        {
            return parameters.DidParameterChange(parameterName, parameterValue);
        }
    }
}
