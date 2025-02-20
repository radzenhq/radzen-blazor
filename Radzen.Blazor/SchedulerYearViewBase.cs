using System;
using Radzen;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A base class for <see cref="RadzenScheduler{TItem}" /> views.
    /// </summary>
    public abstract class SchedulerYearViewBase : SchedulerViewBase
    {
        /// <summary>
        /// Gets the StartMonth of the view.
        /// </summary>
        /// <value>The start month.</value>
        public abstract Month StartMonth { get; set; }

        /// <summary>
        /// Called by the Blazor runtime when parameters are set.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(StartMonth), StartMonth))
            {
                if (Scheduler != null)
                {
                    await Scheduler.Reload();
                }
            }

            await base.SetParametersAsync(parameters);
        }
    }
}