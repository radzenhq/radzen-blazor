using System;
using System.Collections.Generic;
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
    /// Gets or sets the innermost resource item of the slot where the appointment is moved. Set when the slot belongs to a resource in a view which groups
    /// appointments by resource via <see cref="SchedulerViewBase.GroupByResource" />; otherwise <c>null</c>.
    /// </summary>
    public object? Resource { get; set; }

    /// <summary>
    /// Gets or sets the resource items of the slot where the appointment is moved keyed by resource type <see cref="RadzenSchedulerResource.Name" /> -
    /// one entry per grouping level. Set when the slot belongs to a resource in a view which groups appointments by resource; otherwise <c>null</c>.
    /// </summary>
    public IDictionary<string, object>? Resources { get; set; }
}

