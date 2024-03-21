using System;
using Radzen;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A base class for <see cref="RadzenScheduler{TItem}" /> views.
    /// </summary>
    public abstract class SchedulerViewBase : ComponentBase, ISchedulerView, IDisposable
    {
        /// <summary>
        /// Gets the title of the view. It is displayed in the RadzenScheduler title area.
        /// </summary>
        /// <value>The title.</value>
        public abstract string Title { get; }

        /// <summary>
        /// Gets the icon of the view. It is displayed in the view switching UI.
        /// </summary>
        public abstract string Icon { get; }

        /// <summary>
        /// Gets the text of the view. It is displayed in the view switching UI.
        /// </summary>
        /// <value>The text.</value>
        public abstract string Text { get; set; }

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
            Scheduler?.RemoveView(this);
        }

        /// <summary>
        /// Called by the Blazor runtime when parameters are set.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
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
        /// Returns a new date when the user clicks the next button of RadzenScheduler.
        /// </summary>
        /// <returns>The next date. For example a day view will return the next day, a week view will return the next week.</returns>
        public abstract DateTime Next();

        /// <summary>
        /// Returns a new date when the user clicks the previous button of RadzenScheduler.
        /// </summary>
        /// <returns>The previous date. For example a day view will return the previous day, a week view will return the previous week.</returns>
        public abstract DateTime Prev();

        /// <summary>
        /// Renders this instance.
        /// </summary>
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

        /// <summary>
        /// Handles appointent move event.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task OnAppointmentMove(SchedulerAppointmentMoveEventArgs data)
        {
            await Scheduler.AppointmentMove.InvokeAsync(data);
        }
    }
}