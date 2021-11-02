using System;
using Radzen.Blazor;

namespace Radzen
{
    /// <summary>
    /// Supplies information about a <see cref="RadzenScheduler{TItem}.AppointmentSelect" /> event that is being raised.
    /// </summary>
    /// <typeparam name="TItem">The type of the data item.</typeparam>
    public class SchedulerAppointmentSelectEventArgs<TItem>
    {
        /// <summary>
        /// The start date of the appointment.
        /// </summary>
        public DateTime Start { get; set; }
        /// <summary>
        /// The end date of the appointment.
        /// </summary>
        public DateTime End { get; set; }
        /// <summary>
        /// The data item for which the appointment is created.
        /// </summary>
        /// <value>The data.</value>
        public TItem Data { get; set; }
    }
}