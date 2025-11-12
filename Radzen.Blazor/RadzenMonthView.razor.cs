using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;

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
    public partial class RadzenMonthView : SchedulerViewBase
    {
        /// <inheritdoc />
        public override string Icon => "calendar_view_month";

        /// <inheritdoc />
        public override string Title
        {
            get => Scheduler?.CurrentDate.ToString("MMMM yyyy", Scheduler.Culture ?? System.Globalization.CultureInfo.CurrentCulture) ?? "";
        }

        /// <inheritdoc />
        [Parameter]
        public override string Text { get; set; } = "Month";

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
                return Scheduler?.CurrentDate.Date.StartOfMonth().StartOfWeek(Scheduler.Culture ?? System.Globalization.CultureInfo.CurrentCulture) ?? DateTime.Today.StartOfMonth().StartOfWeek(System.Globalization.CultureInfo.CurrentCulture);
            }
        }

        /// <inheritdoc />
        public override DateTime EndDate
        {
            get
            {
                return Scheduler?.CurrentDate.Date.EndOfMonth().EndOfWeek(Scheduler.Culture ?? System.Globalization.CultureInfo.CurrentCulture).AddDays(1) ?? DateTime.Today.EndOfMonth().EndOfWeek(System.Globalization.CultureInfo.CurrentCulture).AddDays(1);
            }
        }

        /// <inheritdoc />
        public override DateTime Next()
        {
            return Scheduler?.CurrentDate.Date.StartOfMonth().AddMonths(1) ?? DateTime.Today.StartOfMonth().AddMonths(1);
        }

        /// <inheritdoc />
        public override DateTime Prev()
        {
            return Scheduler?.CurrentDate.Date.StartOfMonth().AddMonths(-1) ?? DateTime.Today.StartOfMonth().AddMonths(-1);
        }
    }
}
