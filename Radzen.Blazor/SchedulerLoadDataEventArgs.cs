using System;

namespace Radzen
{
    /// <summary>
    /// Class SchedulerLoadDataEventArgs.
    /// </summary>
    public class SchedulerLoadDataEventArgs
    {
        /// <summary>
        /// Gets or sets the start.
        /// </summary>
        /// <value>The start.</value>
        public DateTime Start { get; set; }
        /// <summary>
        /// Gets or sets the end.
        /// </summary>
        /// <value>The end.</value>
        public DateTime End { get; set; }
    }
}