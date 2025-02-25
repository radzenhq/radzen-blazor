using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// A base class for <see cref="MonthView" /> <see cref="DayView" /> <see cref="WeekView" /> <see cref="YearPlannerView" /> <see cref="YearTimelineView" /> views.
    /// </summary>
    public abstract class DropableViewBase : ComponentBase
    {
        /// <summary>
        /// Gets or sets the root view for this. Used to hold and maintain DraggedAppointment and DragStarted variables.
        /// </summary>
        /// <value>The root view associated with this.</value>
        [CascadingParameter]
        public SchedulerViewBase Root { get; set; }

        /// <summary>
        /// Gets or sets the appointment move event callback.
        /// </summary>
        /// <value>The appointment move event callback.</value>
        [Parameter]
        public EventCallback<SchedulerAppointmentMoveEventArgs> AppointmentMove { get; set; }

        /// <summary>
        /// Handles on slot drop.
        /// </summary>
        /// <param name="slotDate"></param>
        /// <param name="resourceFilterList"></param>
        /// <returns>Task</returns>
        public async Task OnDrop(DateTime slotDate, IList<(string Field, string Value)> resourceFilterList = default)
        {
            if (Root.DraggedAppointment != null)
            {
                TimeSpan timespan = slotDate - Root.DraggedAppointment.Start;

                await AppointmentMove.InvokeAsync(new SchedulerAppointmentMoveEventArgs { Appointment = Root.DraggedAppointment, TimeSpan = timespan, ResourceFilters = resourceFilterList });

                Root.DraggedAppointment = null;
            }

            Root.DragStarted = false;
        }

        /// <summary>
        /// Handles Appointment drag started.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public void OnAppointmentDragStart(AppointmentData data)
        {
            if (!Root.DragStarted)
            {
                Root.DragStarted = true;
                Root.DraggedAppointment = data;
            }
        }
    }
}
