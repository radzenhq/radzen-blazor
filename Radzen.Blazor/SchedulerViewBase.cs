using System;
using Radzen;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class SchedulerViewBase.
    /// Implements the <see cref="ComponentBase" />
    /// Implements the <see cref="Radzen.Blazor.ISchedulerView" />
    /// Implements the <see cref="IDisposable" />
    /// </summary>
    /// <seealso cref="ComponentBase" />
    /// <seealso cref="Radzen.Blazor.ISchedulerView" />
    /// <seealso cref="IDisposable" />
    public abstract class SchedulerViewBase : ComponentBase, ISchedulerView, IDisposable
    {
        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <value>The title.</value>
        public abstract string Title { get; }
        /// <summary>
        /// Gets the text.
        /// </summary>
        /// <value>The text.</value>
        public abstract string Text { get; set; }

        /// <summary>
        /// Gets or sets the scheduler.
        /// </summary>
        /// <value>The scheduler.</value>
        [CascadingParameter]
        public IScheduler Scheduler { get; set; }

        /// <summary>
        /// Disposes this instance.
        /// </summary>
        public void Dispose()
        {
            Scheduler?.RemoveView(this);
        }

        /// <summary>
        /// Set parameters as an asynchronous operation.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            if (parameters.DidParameterChange(nameof(Text), Text))
            {
                if (Scheduler != null)
                {
                    await Scheduler.Reload();
                }
            }

            await base.SetParametersAsync(parameters);

            await Scheduler.AddView(this);
        }

        /// <summary>
        /// Nexts this instance.
        /// </summary>
        /// <returns>DateTime.</returns>
        public abstract DateTime Next();

        /// <summary>
        /// Previouses this instance.
        /// </summary>
        /// <returns>DateTime.</returns>
        public abstract DateTime Prev();

        /// <summary>
        /// Renders this instance.
        /// </summary>
        /// <returns>RenderFragment.</returns>
        public abstract RenderFragment Render();

        /// <summary>
        /// Gets the start date.
        /// </summary>
        /// <value>The start date.</value>
        public abstract DateTime StartDate { get; }
        /// <summary>
        /// Gets the end date.
        /// </summary>
        /// <value>The end date.</value>
        public abstract DateTime EndDate { get; }
    }
}