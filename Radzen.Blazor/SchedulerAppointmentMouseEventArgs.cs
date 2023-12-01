using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Supplies information about a <see cref="RadzenScheduler{TItem}.AppointmentMouseEnter" /> or <see cref="RadzenScheduler{TItem}.AppointmentMouseLeave" /> event that is being raised.
    /// </summary>
    /// <typeparam name="TItem">The type of the data item.</typeparam>
    public class SchedulerAppointmentMouseEventArgs<TItem>
    {
        /// <summary>
        /// A reference to the DOM element of the appointment that triggered the event.
        /// </summary>
        public ElementReference Element { get; set; }
        /// <summary>
        /// The data item for which the appointment is created.
        /// </summary>
        /// <value>The data.</value>
        public TItem Data { get; set; }
    }
}
