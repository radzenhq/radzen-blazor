using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;

namespace Radzen
{
    /// <summary>
    /// Supplies information about a <see cref="RadzenResourceScheduler{TItem}.SlotRender" /> event that is being raised.
    /// </summary>
    public class ResourceSchedulerSlotRenderEventArgs<TResource>
    {
        /// <summary>
        /// The resource associated of the slot.
        /// </summary>
        public TResource Resource { get; set; }
        /// <summary>
        /// The start of the slot.
        /// </summary>
        public DateTime Start { get; set; }
        /// <summary>
        /// The end of the slot.
        /// </summary>
        public DateTime End { get; set; }
        /// <summary>
        /// HTML attributes to apply to the slot element.
        /// </summary>
        public IDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
        /// <summary>
        /// The current view.
        /// </summary>
        public ISchedulerView View { get; set;}
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenResourceScheduler{TItem}.SlotSelect" /> event that is being raised.
    /// </summary>
    public class ResourceSchedulerSlotSelectEventArgs<TResource>
    {
        /// <summary>
        /// The resource associated of the slot.
        /// </summary>
        public TResource Resource { get; set; }
        /// <summary>
        /// The start of the slot.
        /// </summary>
        public DateTime Start { get; set; }
        /// <summary>
        /// The end of the slot.
        /// </summary>
        public DateTime End { get; set; }
        /// <summary>
        /// List of appointments.
        /// </summary>
        public IEnumerable<AppointmentData> Appointments { get; set; }
        /// <summary>
        /// Current View.
        /// </summary>
        public ISchedulerView View { get; set; }
        /// <summary>
        /// Has default action been prevented from occuring?
        /// </summary>
        public bool IsDefaultPrevented { get; internal set; }
        /// <summary>
        /// Prevent default action from occuring.
        /// </summary>
        public void PreventDefault()
        {
            IsDefaultPrevented = true;
        }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenResourceScheduler{TItem}.AppointmentRender" /> event that is being raised.
    /// </summary>
    /// <typeparam name="TItem">The type of the data item.</typeparam>
    /// <typeparam name="TResource">The type of the resource.</typeparam>
    public class ResourceSchedulerAppointmentRenderEventArgs<TResource, TItem>
    {
        /// <summary>
        /// The resource associated of the slot.
        /// </summary>
        public TResource Resource { get; set; }
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

    /// <summary>
    /// Supplies information about a <see cref="RadzenResourceScheduler{TItem}.AppointmentSelect" /> event that is being raised.
    /// </summary>
    /// <typeparam name="TItem">The type of the data item.</typeparam>
    /// <typeparam name="TResource">The type of the resource.</typeparam>
    public class ResourceSchedulerAppointmentSelectEventArgs<TResource, TItem>
    {
        /// <summary>
        /// The resource associated of the slot.
        /// </summary>
        public TResource Resource { get; set; }
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
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenResourceScheduler{TItem}.AppointmentMouseEnter" /> or <see cref="RadzenResourceScheduler{TItem}.AppointmentMouseLeave" /> event that is being raised.
    /// </summary>
    /// <typeparam name="TItem">The type of the data item.</typeparam>
    /// <typeparam name="TResource">The type of the resource.</typeparam>
    public class ResourceSchedulerAppointmentMouseEventArgs<TResource, TItem>
    {
        /// <summary>
        /// The resource associated of the slot.
        /// </summary>
        public TResource Resource { get; set; }
        /// <summary>
        /// A reference to the DOM element of the appointment that triggered the event.
        /// </summary>
        public ElementReference Element { get; set; }
        /// <summary>
        /// The data item for which the appointment is created.
        /// </summary>
        /// <value>The data.</value>
        public TItem Data { get; set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenResourceScheduler{TItem}.MonthSelect" /> event that is being raised.
    /// </summary>
    /// <typeparam name="TResource">The type of the resource.</typeparam>
    public class ResourceSchedulerMonthSelectEventArgs<TResource>
    {
        /// <summary>
        /// The resource associated of the slot.
        /// </summary>
        public TResource Resource { get; set; }
        /// <summary>
        /// Month start date.
        /// </summary>
        public DateTime MonthStart { get; set; }
        /// <summary>
        /// List of appointments.
        /// </summary>
        public IEnumerable<AppointmentData> Appointments { get; set; }
        /// <summary>
        /// Current View.
        /// </summary>
        public ISchedulerView View { get; set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenResourceScheduler{TItem}.MonthSelect" /> event that is being raised.
    /// </summary>
    /// <typeparam name="TResource">The type of the resource.</typeparam>
    public class ResourceSchedulerDaySelectEventArgs<TResource>
    {
        /// <summary>
        /// The resource associated of the slot.
        /// </summary>
        public TResource Resource { get; set; }
        /// <summary>
        /// Selected date.
        /// </summary>
        public DateTime Day { get; set; }
        /// <summary>
        /// List of appointments.
        /// </summary>
        public IEnumerable<AppointmentData> Appointments { get; set; }
        /// <summary>
        /// Current View.
        /// </summary>
        public ISchedulerView View { get; set; }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenResourceScheduler{TItem}.MoreSelect" /> event that is being raised.
    /// </summary>
    /// <typeparam name="TResource">The type of the resource.</typeparam>
    public class ResourceSchedulerMoreSelectEventArgs<TResource>
    {
        /// <summary>
        /// The resource associated of the slot.
        /// </summary>
        public TResource Resource { get; set; }
        /// <summary>
        /// The start of the slot.
        /// </summary>
        public DateTime Start { get; set; }
        /// <summary>
        /// The end of the slot.
        /// </summary>
        public DateTime End { get; set; }
        /// <summary>
        /// List of appointments.
        /// </summary>
        public IEnumerable<AppointmentData> Appointments { get; set; }
        /// <summary>
        /// Current View.
        /// </summary>
        public ISchedulerView View { get; set; }
        /// <summary>
        /// Has default action been prevented from occuring.
        /// </summary>
        public bool IsDefaultPrevented { get; internal set; }
        /// <summary>
        /// Prevent the default action from occuring.
        /// </summary>
        public void PreventDefault()
        {
            IsDefaultPrevented = true;
        }
    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenResourceScheduler{TItem}.LoadData" /> event that is being raised.
    /// </summary>
    /// <typeparam name="TResource">The type of the resource.</typeparam>
    public class ResourceSchedulerLoadDataEventArgs<TResource>
    {
        /// <summary>
        /// The resource associated of the slot.
        /// </summary>
        public TResource Resource { get; set; }
        /// <summary>
        /// The start of the currently rendered period.
        /// </summary>
        public DateTime Start { get; set; }
        /// <summary>
        /// The start of the currently rendered period.
        /// </summary>
        public DateTime End { get; set; }
        /// <summary>
        /// The selected view of the scheduler.
        /// </summary>
        public ISchedulerView View { get; set; }

    }

    /// <summary>
    /// Supplies information about a <see cref="RadzenResourceScheduler{TItem}.AppointmentMove" /> or <see cref="RadzenResourceScheduler{TItem}.AppointmentMove" /> event that is being raised.
    /// </summary>
    /// <typeparam name="TItem">The type of the data item.</typeparam>
    /// <typeparam name="TResource">The type of the resource.</typeparam>
    public class ResourceSchedulerAppointmentMoveEventArgs<TResource, TItem>
    {
        /// <summary>
        /// The resource associated of the slot.
        /// </summary>
        public TResource Resource { get; set; }
        /// <summary>
        /// The data item for which the appointment is created.
        /// </summary>
        /// <value>The data.</value>
        public TItem Data { get; set; }
        /// <summary>
        /// The start of the slot.
        /// </summary>
        public DateTime DestinationStart { get; set; }
        /// <summary>
        /// The end of the slot.
        /// </summary>
        public DateTime DestinationEnd { get; set; }
        /// <summary>
        /// The selected view of the scheduler.
        /// </summary>
        public ISchedulerView View { get; set; }
        /// <summary>
        /// Has default action been prevented from occuring.
        /// </summary>
        public bool IsDefaultPrevented { get; internal set; }
        /// <summary>
        /// Prevent the default action from occuring.
        /// </summary>
        public void PreventDefault()
        {
            IsDefaultPrevented = true;
        }
    }

}