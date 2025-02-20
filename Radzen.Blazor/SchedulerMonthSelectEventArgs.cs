using System;
using System.Collections.Generic;
using Radzen.Blazor;

namespace Radzen
{
    /// <summary>
    /// Supplies information about a <see cref="RadzenScheduler{TItem}.MonthSelect" /> event that is being raised.
    /// </summary>
    public class SchedulerMonthSelectEventArgs
    {
        /// <summary>
        /// Month start date.
        /// </summary>
        public DateTime MonthStart { get; set; }
        /// <summary>
        /// List of appointments.
        /// </summary>
        public IEnumerable<AppointmentData> Appointments { get; set; }
        /// <summary>
        /// Current View.
        /// </summary>
        public ISchedulerView View { get; set; }
    }
}