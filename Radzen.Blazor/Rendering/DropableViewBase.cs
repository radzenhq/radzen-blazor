using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// A base class for <see cref="MonthView" /> <see cref="DayView" /> <see cref="WeekView" /> <see cref="YearPlannerView" /> <see cref="YearTimelineView" /> views.
    /// </summary>
    public abstract class DropableViewBase : ComponentBase
    {
        private bool dragStarted = false;
        private AppointmentData draggedAppointment;
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
        /// <returns>Task</returns>
        public async Task OnDrop(DateTime slotDate)
        {
            if (draggedAppointment != null)
            {
                TimeSpan timespan = slotDate - draggedAppointment.Start;

                await AppointmentMove.InvokeAsync(new SchedulerAppointmentMoveEventArgs { Appointment = draggedAppointment, TimeSpan = timespan });

                draggedAppointment = null;
            }

            dragStarted = false;
        }

        /// <summary>
        /// Handles Appointment drag started.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public void OnAppointmentDragStart(AppointmentData data)
        {
            if (!dragStarted)
            {
                dragStarted = true;
                draggedAppointment = data;
            }
        }
    }
}
