using System;

namespace Radzen
{
    /// <summary>
    /// Class SchedulerAppointmentSelectEventArgs.
    /// </summary>
    /// <typeparam name="TItem">The type of the t item.</typeparam>
    public class SchedulerAppointmentSelectEventArgs<TItem>
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
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public TItem Data { get; set; }
    }
}