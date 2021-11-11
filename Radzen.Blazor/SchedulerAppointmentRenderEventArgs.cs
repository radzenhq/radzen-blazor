using System;
using System.Collections.Generic;
using Radzen.Blazor;

namespace Radzen
{
    /// <summary>
    /// Supplies information about a <see cref="RadzenScheduler{TItem}.AppointmentRender" /> event that is being raised.
    /// </summary>
    /// <typeparam name="TItem">The type of the data item.</typeparam>
    public class SchedulerAppointmentRenderEventArgs<TItem>
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
        /// <summary>
        /// HTML attributes to apply to the appointment element.
        /// </summary>
        public IDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
    }
}