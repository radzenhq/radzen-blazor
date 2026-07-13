using System;
using Radzen.Blazor;
using Radzen.Blazor.Rendering;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="DropableViewBase.AppointmentMove" /> event that is being raised.
/// </summary>
public class SchedulerAppointmentMoveEventArgs
{
    /// <summary>
    /// Gets or sets the appointment data.
    /// </summary>
    public AppointmentData? Appointment { get; set; }

    /// <summary>
    /// Gets or sets the time span which represents the difference between slot start and appointment start.
    /// </summary>
    public TimeSpan TimeSpan { get; set; }

    /// <summary>
    /// Gets or sets the date of the slot where the appointment is moved.
    /// </summary>
    public DateTime SlotDate { get; set; }

    /// <summary>
    /// Gets or sets the resource of the slot where the appointment is moved. Set when the slot belongs to a resource in a resource view such as <see cref="RadzenResourceDayView" />; otherwise <c>null</c>.
    /// </summary>
    public object? Resource { get; set; }
}

