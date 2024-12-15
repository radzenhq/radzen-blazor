using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// Displays the appointments in a multi-day view in <see cref="RadzenScheduler{TItem}" />
    /// </summary>
    /// <code>
    /// &lt;RadzenScheduler Data="@appointments"&gt;
    ///     &lt;RadzenMultiDayView /&gt;
    /// &lt;/RadzenScheduler&gt;
    /// </code>
    public partial class RadzenMultiDayView : SchedulerViewBase
    {
        /// <inheritdoc />
        public override string Icon => "width_normal";

        /// <inheritdoc />
        [Parameter]
        public override string Text { get; set; } = "Multi-Day";

        /// <summary>
        /// Gets or sets the time format.
        /// </summary>
        /// <value>The time format. Set to <c>h tt</c> by default.</value>
        [Parameter]
        public string TimeFormat { get; set; } = "h tt";

        /// <summary>
        /// Gets or sets the format used to display the header text.
        /// </summary>
        /// <value>The header text format. Set to <c>ddd</c> by default.</value>
        [Parameter]
        public string HeaderFormat { get; set; } = "ddd";

        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        /// <value>The start time.</value>
        [Parameter]
        public TimeSpan StartTime { get; set; } = TimeSpan.FromHours(8);

        /// <summary>
        /// Gets or sets the end time.
        /// </summary>
        /// <value>The end time.</value>
        [Parameter]
        public TimeSpan EndTime { get; set; } = TimeSpan.FromHours(24);

        /// <summary>
        /// Gets or sets slot size in minutes. Set to <c>30</c> by default.
        /// </summary>
        /// <value>The slot size in minutes.</value>
        [Parameter]
        public int MinutesPerSlot { get; set; } = 30;

        /// <summary>
        /// Gets or sets number of days to view. Set to <c>2</c> by default.
        /// </summary>
        /// <value>The number of days.</value>
        [Parameter]
        public int NumberOfDays { get; set; } = 2;

        /// <summary>
        /// Gets or sets number of days to advance when using prev / next. Set to <c>1</c> by default.
        /// </summary>
        /// <value>The number of days to advance.</value>
        [Parameter]
        public int AdvanceDays { get; set; } = 1;
        /// <inheritdoc />
        public override DateTime StartDate
        {
            get
            {
                return Scheduler.CurrentDate.Date;
            }
        }

        /// <inheritdoc />
        public override DateTime EndDate
        {
            get
            {
                return StartDate.AddDays(NumberOfDays);
            }
        }

        /// <inheritdoc />
        public override string Title
        {
            get
            {
                if (StartDate == EndDate.AddDays(-1))
                {
                    return $"{StartDate.ToString(Scheduler.Culture.DateTimeFormat.ShortDatePattern)}";
                }
                else
                {
                    return $"{StartDate.ToString(Scheduler.Culture.DateTimeFormat.ShortDatePattern)} - {EndDate.AddDays(-1).ToString(Scheduler.Culture.DateTimeFormat.ShortDatePattern)}";
                }
            }
        }


        /// <inheritdoc />
        public override DateTime Next()
        {
            return Scheduler.CurrentDate.Date.AddDays(AdvanceDays);
        }

        /// <inheritdoc />
        public override DateTime Prev()
        {
            return Scheduler.CurrentDate.Date.AddDays(-AdvanceDays);
        }
    }
}
