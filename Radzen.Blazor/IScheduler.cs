using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// The common <see cref="RadzenScheduler{TItem}" /> API injected as a cascading parameter to is views.
    /// </summary>
    public interface IScheduler
    {
        /// <summary>
        /// Gets the appointments in the specified range.
        /// </summary>
        /// <param name="start">The start of the range.</param>
        /// <param name="end">The end of the range.</param>
        /// <returns>A collection of appointments within the specified range.</returns>
        IEnumerable<AppointmentData> GetAppointmentsInRange(DateTime start, DateTime end);
        /// <summary>
        /// Determines whether an appointment is within the specified range.
        /// </summary>
        /// <param name="item">The appointment to check.</param>
        /// <param name="start">The start of the range.</param>
        /// <param name="end">The end of the range.</param>
        /// <returns><c>true</c> if the appointment is within the specified range; otherwise, <c>false</c>.</returns>
        bool IsAppointmentInRange(AppointmentData item, DateTime start, DateTime end);
        /// <summary>
        /// Adds a view. Must be called when a <see cref="ISchedulerView" /> is initialized.
        /// </summary>
        /// <param name="view">The view to add.</param>
        Task AddView(ISchedulerView view);
        /// <summary>
        /// Removes a view. Must be called when a <see cref="ISchedulerView" /> is disposed.
        /// </summary>
        /// <param name="view">The view to remove.</param>
        void RemoveView(ISchedulerView view);
        /// <summary>
        /// Determines whether the specified view is selected.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <returns><c>true</c> if the specified view is selected; otherwise, <c>false</c>.</returns>
        bool IsSelected(ISchedulerView view);
        /// <summary>
        /// Gets or sets the current date.
        /// </summary>
        /// <value>The current date.</value>
        DateTime CurrentDate { get; set; }
        /// <summary>
        /// Selects the specified appointment.
        /// </summary>
        /// <param name="data">The appointment to select.</param>
        Task SelectAppointment(AppointmentData data);
        /// <summary>
        /// Selects the specified slot.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        Task SelectSlot(DateTime start, DateTime end);
        /// <summary>
        /// Selects the specified slot.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="appointments">The appointments for this range.</param>
        Task<bool> SelectSlot(DateTime start, DateTime end, IEnumerable<AppointmentData> appointments);
        /// <summary>
        /// Selects the specified more link.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="appointments">The appointments for this range.</param>
        Task<bool> SelectMore(DateTime start, DateTime end, IEnumerable<AppointmentData> appointments);
        /// <summary>
        /// Gets the appointment HTML attributes.
        /// </summary>
        /// <param name="item">The appointment.</param>
        /// <returns>A dictionary containing the HTML attributes for the specified appointment.</returns>
        IDictionary<string, object> GetAppointmentAttributes(AppointmentData item);
        /// <summary>
        /// Gets the slot HTML attributes.
        /// </summary>
        /// <param name="start">The start of the slot.</param>
        /// <param name="end">The end of the slot.</param>
        /// <returns>A dictionary containing the HTML attributes for the specified slot.</returns>
        IDictionary<string, object> GetSlotAttributes(DateTime start, DateTime end);
        /// <summary>
        /// Renders the appointment.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>RenderFragment.</returns>
        RenderFragment RenderAppointment(AppointmentData item);
        /// <summary>
        /// Reloads this instance.
        /// </summary>
        Task Reload();
        /// <summary>
        /// Gets the height.
        /// </summary>
        /// <value>The height.</value>
        double Height { get; }
        /// <summary>
        /// Gets or sets the culture.
        /// </summary>
        /// <value>The culture.</value>
        CultureInfo Culture { get; set; }
    }
}