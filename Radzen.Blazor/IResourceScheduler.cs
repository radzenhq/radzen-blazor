using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// The common <see cref="RadzenResourceScheduler{TResource, TItem, TLink}" /> API injected as a cascading parameter to is views.
    /// </summary>
    public interface IResourceScheduler
    {
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
        /// Gets or sets the culture.
        /// </summary>
        /// <value>The culture.</value>
        CultureInfo Culture { get; set; }
    }
}