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
                return Scheduler == null ? DateTime.Today : GetYearRange().viewStart;
            }
        }

        /// <inheritdoc />
        public override DateTime EndDate
        {
            get
            {
                return Scheduler == null ? DateTime.Today : GetYearRange().viewEnd;
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
