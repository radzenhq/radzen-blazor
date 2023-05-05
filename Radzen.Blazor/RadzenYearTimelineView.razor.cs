using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Drawing;
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
    public partial class RadzenYearTimelineView : SchedulerViewBase
    {
        /// <inheritdoc />
        public override string Icon => "view_timeline";

        /// <inheritdoc />
        public override string Title
        {
            get => Scheduler.CurrentDate.ToString("yyyy", Scheduler.Culture);
        }

        /// <inheritdoc />
        [Parameter]
        public override string Text { get; set; } = "Timeline";

        /// <summary>
        /// Specifies the maximum appointnments to render in a slot.
        /// </summary>
        /// <value>The maximum appointments in slot.</value>
        [Parameter]
        public int? MaxAppointmentsInSlot { get; set; }

        /// <summary>
        /// Specifies the text displayed when there are more appointments in a slot than <see cref="MaxAppointmentsInSlot" />.
        /// </summary>
        /// <value>The more text. Set to <c>"+ {0} more"</c> by default.</value>
        [Parameter]
        public string MoreText { get; set; } = "+ {0} more";

        /// <inheritdoc />
        public override DateTime StartDate
        {
            get
            {
                var d = new DateTime(Scheduler.CurrentDate.Date.Year, 1, 1).StartOfWeek();
                if (d.DayOfWeek == DateTimeFormatInfo.CurrentInfo.FirstDayOfWeek) d.AddDays(-7);

                return d;
            }
        }

        /// <inheritdoc />
        public override DateTime EndDate
        {
            get
            {
                var d = new DateTime(Scheduler.CurrentDate.Date.Year, 1, 1).AddDays(DateTime.IsLeapYear(Scheduler.CurrentDate.Date.Year) ? 366 : 365).EndOfWeek();

                return d;
            }
        }

        /// <inheritdoc />
        public override DateTime Next()
        {
            return Scheduler.CurrentDate.Date.AddYears(1);
        }

        /// <inheritdoc />
        public override DateTime Prev()
        {
            return Scheduler.CurrentDate.Date.AddYears(-1);
        }
    }
}
