using System;
using System.Collections.Generic;
using Radzen.Blazor;

namespace Radzen
{
    /// <summary>
    /// Supplies information about a <see cref="RadzenScheduler{TItem}.SlotRender" /> event that is being raised.
    /// </summary>
    public class SchedulerSlotRenderEventArgs
    {
        /// <summary>
        /// The start of the slot.
        /// </summary>
        public DateTime Start { get; set; }
        /// <summary>
        /// The end of the slot.
        /// </summary>
        public DateTime End { get; set; }
        // used to pass a function to get appointments on demand.
        internal Func<IEnumerable<AppointmentData>>? getAppointments;
        private IEnumerable<AppointmentData>? appointments;
        /// <summary>
        /// List of appointments.
        /// </summary>
        public IEnumerable<AppointmentData> Appointments => appointments ??= getAppointments!();
        /// <summary>
        /// HTML attributes to apply to the slot element.
        /// </summary>
        public IDictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
        /// <summary>
        /// The current view.
        /// </summary>
        public ISchedulerView? View { get; set;}
        /// <summary>
        /// The innermost resource item associated with the slot. Set when the slot belongs to a resource in a view which groups appointments by resource
        /// via <see cref="SchedulerViewBase.GroupByResource" />; otherwise <c>null</c>.
        /// </summary>
        public object? Resource { get; set; }
        /// <summary>
        /// The resource items associated with the slot keyed by resource type <see cref="RadzenSchedulerResource.Name" /> - one entry per grouping level.
        /// Set when the slot belongs to a resource in a view which groups appointments by resource; otherwise <c>null</c>.
        /// </summary>
        public IDictionary<string, object>? Resources { get; set; }

    }
}