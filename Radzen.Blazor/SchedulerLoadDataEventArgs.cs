using System;
using Radzen.Blazor;

namespace Radzen
{
    /// <summary>
    /// Supplies information about a <see cref="RadzenScheduler{TItem}.LoadData" /> event that is being raised.
    /// </summary>
    public class SchedulerLoadDataEventArgs
    {
        /// <summary>
        /// The start of the currently rendered period.
        /// </summary>
        public DateTime Start { get; set; }
        /// <summary>
        /// The start of the currently rendered period.
        /// </summary>
        public DateTime End { get; set; }
    }
}