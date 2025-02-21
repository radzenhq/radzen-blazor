using System;
using Radzen;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;


namespace Radzen.Blazor
{
    /// <summary>
    /// A base class for <see cref="RadzenSchedulerResource{TItem}" /> views.
    /// </summary>
    public abstract class SchedulerResourceBase : ComponentBase, ISchedulerResource, IDisposable
    {
        /// <summary>
        /// Gets the title of the resource.
        /// </summary>
        /// <value>The title.</value>
        public abstract string Title { get; set; }

        /// <summary>
        /// Gets the property that is used in appointment data to represent this resource
        /// </summary>
        /// <value>The property.</value>
        public abstract string Property { get; set; }

        /// <summary>
        /// Gets the order of grouping.
        /// </summary>
        /// <value>The order.</value>
        public abstract int Order { get; set; }
        /// <summary>
        /// Gets or sets whether this resource is active in grouping.
        /// </summary>
        /// <value>Is active.</value>
        public abstract bool IsActive { get; set; }
        /// <summary>
        /// Gets the resource data
        /// </summary>
        /// <value>The property.</value>
        public abstract IEnumerable<string> Data { get; set; }

        /// <summary>
        /// Gets or sets the scheduler instance.
        /// </summary>
        /// <value>The scheduler.</value>
        [CascadingParameter]
        public IScheduler Scheduler { get; set; }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Scheduler?.RemoveResource(this);
        }

        /// <summary>
        /// Called by the Blazor runtime when parameters are set.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(IsActive), IsActive) || parameters.DidParameterChange(nameof(Order), Order))
            {
                if (Scheduler != null)
                {
                    await Scheduler.Reload();
                }
            }

            await base.SetParametersAsync(parameters);

            Scheduler.AddResource(this);
        }
    }
}