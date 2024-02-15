using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// A base class for <see cref="MonthView" /> <see cref="DayView" /> <see cref="WeekView" /> views.
    /// </summary>
    public abstract class ViewBase : ComponentBase
    {
        private bool dragStarted = false;
        private AppointmentData draggedAppointment;
        /// <summary>
        /// Gets or sets the appointment move event callback.
        /// </summary>
        /// <value>The appointment move event callback.</value>
        [Parameter]
        public EventCallback<AppointmentMoveEventArgs> AppointmentMove { get; set; }
        
        /// <summary>
        /// Handles on slot drop.
        /// </summary>
        /// <param name="slotDate"></param>
        /// <returns>Task</returns>
        public async Task OnDrop(DateTime slotDate)
        {
            dragStarted = false;
            TimeSpan timespan = slotDate - draggedAppointment.Start;
            await AppointmentMove.InvokeAsync(new AppointmentMoveEventArgs() { Appointment = draggedAppointment, TimeSpan = timespan });
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
