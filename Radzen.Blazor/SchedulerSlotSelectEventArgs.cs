using System;
using System.Collections.Generic;
using Radzen.Blazor;

namespace Radzen
{
    /// <summary>
    /// Supplies information about a <see cref="RadzenScheduler{TItem}.SlotSelect" /> event that is being raised.
    /// </summary>
    public class SchedulerSlotSelectEventArgs
    {
        /// <summary>
        /// The start of the slot.
        /// </summary>
        public DateTime Start { get; set; }
        /// <summary>
        /// The end of the slot.
        /// </summary>
        public DateTime End { get; set; }
        /// <summary>
        /// List of appointments.
        /// </summary>
        public IEnumerable<AppointmentData> Appointments { get; set; }
    }
}