using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Globalization;

namespace Radzen.Blazor
{
    /// <summary>
    /// Displays the appointments in a month day in <see cref="RadzenScheduler{TItem}" />
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenScheduler Data="@appointments"&gt;
    ///     &lt;RadzenMonthView /&gt;
    /// &lt;/RadzenScheduler&gt;
    /// </code>
    /// </example>
    public partial class RadzenYearView : SchedulerYearViewBase
    {
        /// <inheritdoc />
        public override string Icon => "calendar_month";

        /// <inheritdoc />
        public override string Title
        {
            get
            {
                if (Scheduler == null) return "";
                var culture = Scheduler.Culture ?? System.Globalization.CultureInfo.CurrentCulture;
                if (StartMonth == Month.January)
                {
                    return Scheduler.CurrentDate.ToString("yyyy", culture);
                }
                else
                {
                    return (Scheduler.CurrentDate.Month < (int)StartMonth + 1) ? $"{Scheduler.CurrentDate.AddYears(-1).ToString("yyyy", culture)}-{Scheduler.CurrentDate.ToString("yyyy", culture)}" : $"{Scheduler.CurrentDate.ToString("yyyy", culture)}-{Scheduler.CurrentDate.AddYears(+1).ToString("yyyy", culture)}";
                }
            }
        }

        /// <inheritdoc />
        [Parameter]
        public override string Text { get; set; } = "Year";

        /// <summary>
        /// Specifies the text displayed when there are more appointments in a slot than MaxAppointmentsInSlot.
        /// </summary>
        /// <value>The more text. Set to <c>"+ {0} more"</c> by default.</value>
        [Parameter]
        public string MoreText { get; set; } = "+ {0} more";

        /// <summary>
        /// Specifies the text displayed when the user clicks on a day with no events in the year view
        /// </summary>
        [Parameter]
        public string NoDayEventsText { get; set; } = "There are no scheduled events taking place on this day";

        /// <inheritdoc />
        public override DateTime StartDate
        {
            get
            {
                if (Scheduler == null) return DateTime.Today;
                var culture = Scheduler.Culture ?? System.Globalization.CultureInfo.CurrentCulture;
                if (StartMonth == Month.January)
                {
                    var d = new DateTime(Scheduler.CurrentDate.Date.Year, 1, 1).StartOfWeek(culture);
                    if (d.DayOfWeek == culture.DateTimeFormat.FirstDayOfWeek) d = d.AddDays(-7);
                    return d;
                }
                else
                {
                    var d = new DateTime(Scheduler.CurrentDate.Date.Year + (Scheduler.CurrentDate.Month < (int)StartMonth + 1 ? -1 : 0), (int)StartMonth + 1, 1).StartOfWeek(culture);
                    if (d.DayOfWeek == culture.DateTimeFormat.FirstDayOfWeek) d = d.AddDays(-7);
                    return d;
                }
            }
        }

        /// <inheritdoc />
        public override DateTime EndDate
        {
            get
            {
                if (Scheduler == null) return DateTime.Today;
                var culture = Scheduler.Culture ?? System.Globalization.CultureInfo.CurrentCulture;
                var realFirstYear = StartDate.AddDays(7);
                var d = StartDate.AddDays(DateTime.IsLeapYear(realFirstYear.Year) || DateTime.IsLeapYear(realFirstYear.Year + 1) ? 366 : 365).EndOfWeek(culture);
                return d;
            }
        }

        /// <summary>
        /// Gets or sets the start month for the year views />.
        /// </summary>
        /// <value>The start month.</value>
        [Parameter]
        public override Month StartMonth { get; set; } = Month.January;

        /// <inheritdoc />
        public override DateTime Next()
        {
            return Scheduler?.CurrentDate.Date.AddYears(1) ?? DateTime.Today.AddYears(1);
        }

        /// <inheritdoc />
        public override DateTime Prev()
        {
            return Scheduler?.CurrentDate.Date.AddYears(-1) ?? DateTime.Today.AddYears(-1);
        }
    }
}
