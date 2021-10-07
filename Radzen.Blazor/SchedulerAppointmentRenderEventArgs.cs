using System;
using System.Collections.Generic;

namespace Radzen
{
    /// <summary>
    /// Class SchedulerAppointmentRenderEventArgs.
    /// </summary>
    /// <typeparam name="TItem">The type of the t item.</typeparam>
    public class SchedulerAppointmentRenderEventArgs<TItem>
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
        /// <summary>
        /// Gets or sets the attributes.
        /// </summary>
        /// <value>The attributes.</value>
        public IDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
    }
}