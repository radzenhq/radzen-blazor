using System;
using Radzen.Blazor;

namespace Radzen
{
    /// <summary>
    /// Supplies information about a <see cref="RadzenScheduler{TItem}.TodaySelect" /> event that is being raised.
    /// </summary>
    public class SchedulerTodaySelectEventArgs
    {
        /// <summary>
        /// Today's date. You can change this value to navigate to a different date.
        /// </summary>
        public DateTime Today { get; set; }
    }
}