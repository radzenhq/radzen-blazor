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
    public partial class RadzenYearPlannerView : SchedulerYearViewBase
    {
        /// <inheritdoc />
        public override string Icon => "view_list";

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
        public override string Text { get; set; } = "Planner";

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
        public string MoreText { get; set; } = "+{0}";

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
