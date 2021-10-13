using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenChartComponentBase.
    /// Implements the <see cref="ComponentBase" />
    /// </summary>
    /// <seealso cref="ComponentBase" />
    public abstract class RadzenChartComponentBase : ComponentBase
    {
        /// <summary>
        /// The chart
        /// </summary>
        private RadzenChart chart;

        /// <summary>
        /// Gets or sets the chart.
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
        /// Initializes this instance.
        /// </summary>
        protected virtual void Initialize()
        {

        }

        /// <summary>
        /// Shoulds the refresh chart.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected virtual bool ShouldRefreshChart(ParameterView parameters)
        {
            return false;
        }

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
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
        /// Dids the parameter change.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="parameters">The parameters.</param>
        /// <param name="parameterName">Name of the parameter.</param>
        /// <param name="parameterValue">The parameter value.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected bool DidParameterChange<T>(ParameterView parameters, string parameterName, T parameterValue)
        {
            return parameters.DidParameterChange(parameterName, parameterValue);
        }
    }
}
