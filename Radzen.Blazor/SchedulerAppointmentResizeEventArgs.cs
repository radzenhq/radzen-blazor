using System;
using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="RadzenScheduler{TItem}.AppointmentResize" /> event that is being raised.
/// </summary>
public class SchedulerAppointmentResizeEventArgs
{
    /// <summary>
    /// Gets or sets the appointment data.
    /// </summary>
    public AppointmentData? Appointment { get; set; }

    /// <summary>
    /// Gets or sets the new start of the appointment produced by dragging its start edge. Equal to the current appointment start when the end edge is dragged.
    /// </summary>
    public DateTime Start { get; set; }

    /// <summary>
    /// Gets or sets the new end of the appointment produced by dragging its end edge. Equal to the current appointment end when the start edge is dragged.
    /// </summary>
    public DateTime End { get; set; }
}
