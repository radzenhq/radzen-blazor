using System;
using Radzen;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using Radzen.Blazor.Rendering;

namespace Radzen.Blazor
{
    /// <summary>
    /// A base class for <see cref="RadzenScheduler{TItem}" /> views.
    /// </summary>
    public abstract class SchedulerYearViewBase : SchedulerViewBase
    {
        /// <summary>
        /// Gets the StartMonth of the view.
        /// </summary>
        /// <value>The start month.</value>
        public abstract Month StartMonth { get; set; }

        /// <summary>
        /// Returns the logical year start (first day of <see cref="StartMonth"/>) and the computed view start/end range.
        /// </summary>
        /// <remarks>
        /// Year views render whole weeks. The view <c>StartDate</c> is the start of the week containing the year start,
        /// and the view <c>EndDate</c> is the end of the week containing the last day of the year range.
        /// </remarks>
        protected (DateTime yearStart, DateTime viewStart, DateTime viewEnd) GetYearRange()
        {
            if (Scheduler == null)
            {
                var today = DateTime.Today;
                return (today, today, today);
            }

            var culture = Scheduler.Culture ?? CultureInfo.CurrentCulture;
            var startMonthNumber = (int)StartMonth + 1; // Month enum is 0-based.

            // Determine which "year" we are showing based on the configured fiscal start month.
            var year = Scheduler.CurrentDate.Date.Year + (Scheduler.CurrentDate.Month < startMonthNumber ? -1 : 0);
            var yearStart = new DateTime(year, startMonthNumber, 1);

            // Render one extra week above when the year starts exactly on the first day of week (matches existing behavior).
            var viewStart = yearStart.StartOfWeek(culture);
            if (viewStart.DayOfWeek == culture.DateTimeFormat.FirstDayOfWeek)
            {
                viewStart = viewStart.AddDays(-7);
            }

            // IMPORTANT: compute the end from the real year end, not from viewStart.
            // Otherwise, when viewStart is in the previous year, we can end up one week short.
            var yearEnd = yearStart.AddYears(1).AddDays(-1);
            // Keep EndDate semantics consistent with other scheduler views: EndDate is exclusive.
            // That is, it points to the start of the day AFTER the last visible day.
            var viewEnd = yearEnd.EndOfWeek(culture).AddDays(1);

            return (yearStart, viewStart, viewEnd);
        }

        /// <summary>
        /// Called by the Blazor runtime when parameters are set.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var startMonthChanged = parameters.DidParameterChange(nameof(StartMonth), StartMonth);

            await base.SetParametersAsync(parameters);

            if (startMonthChanged && Scheduler != null)
            {
                await Scheduler.Reload();
            }
        }
    }
}