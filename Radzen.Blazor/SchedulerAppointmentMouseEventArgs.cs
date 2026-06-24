using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Supplies information about a <see cref="RadzenScheduler{TItem}.AppointmentMouseEnter" /> or <see cref="RadzenScheduler{TItem}.AppointmentMouseLeave" /> event that is being raised.
    /// </summary>
    /// <typeparam name="TItem">The type of the data item.</typeparam>
    public class SchedulerAppointmentMouseEventArgs<TItem>
    {
        /// <summary>
        /// A reference to the DOM element of the appointment that triggered the event.
        /// </summary>
        public ElementReference Element { get; set; }
        /// <summary>
        /// The data item for which the appointment is created.
        /// </summary>
        /// <value>The data.</value>
        public TItem? Data { get; set; }
        /// <summary>
        /// The appointment that triggered the event, including its resolved <see cref="Radzen.Blazor.AppointmentData.Start" />, <see cref="Radzen.Blazor.AppointmentData.End" /> and <see cref="Radzen.Blazor.AppointmentData.Text" />.
        /// </summary>
        /// <value>The appointment data.</value>
        public AppointmentData? AppointmentData { get; set; }
        /// <summary>
        /// The horizontal position (X) of the mouse pointer in viewport coordinates.
        /// </summary>
        public double ClientX { get; set; }
        /// <summary>
        /// The vertical position (Y) of the mouse pointer in viewport coordinates.
        /// </summary>
        public double ClientY { get; set; }
    }
}
