using System;
using System.Collections.Generic;
using Radzen.Blazor;

namespace Radzen
{
    /// <summary>
    /// Supplies information about a <see cref="RadzenScheduler{TItem}.SlotSelect" /> event that is being raised.
    /// </summary>
    public class SchedulerDaySelectEventArgs
    {
        /// <summary>
        /// The date of the selected day.
        /// </summary>
        public DateTime Date { get; set; }
        /// <summary>
        /// List of appointments for the selected day.
        /// </summary>
        public IEnumerable<AppointmentData> Appointments { get; set; }
    }
}