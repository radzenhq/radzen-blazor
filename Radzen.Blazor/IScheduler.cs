using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// Interface IScheduler
    /// </summary>
    public interface IScheduler
    {
        /// <summary>
        /// Gets the appointments in range.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <returns>IEnumerable&lt;AppointmentData&gt;.</returns>
        IEnumerable<AppointmentData> GetAppointmentsInRange(DateTime start, DateTime end);
        /// <summary>
        /// Determines whether [is appointment in range] [the specified item].
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <returns><c>true</c> if [is appointment in range] [the specified item]; otherwise, <c>false</c>.</returns>
        bool IsAppointmentInRange(AppointmentData item, DateTime start, DateTime end);
        /// <summary>
        /// Adds the view.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <returns>Task.</returns>
        Task AddView(ISchedulerView view);
        /// <summary>
        /// Removes the view.
        /// </summary>
        /// <param name="view">The view.</param>
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
        /// Selects the appointment.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>Task.</returns>
        Task SelectAppointment(AppointmentData data);
        /// <summary>
        /// Selects the slot.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <returns>Task.</returns>
        Task SelectSlot(DateTime start, DateTime end);
        /// <summary>
        /// Gets the appointment attributes.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>IDictionary&lt;System.String, System.Object&gt;.</returns>
        IDictionary<string, object> GetAppointmentAttributes(AppointmentData item);
        /// <summary>
        /// Renders the appointment.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>RenderFragment.</returns>
        RenderFragment RenderAppointment(AppointmentData item);
        /// <summary>
        /// Reloads this instance.
        /// </summary>
        /// <returns>Task.</returns>
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