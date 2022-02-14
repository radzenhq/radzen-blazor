using Microsoft.AspNetCore.Components;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// Displays the appointments in a single day in <see cref="RadzenScheduler{TItem}" />
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenScheduler Data="@appointments"&gt;
    ///     &lt;RadzenDayView /&gt;
    /// &lt;/RadzenScheduler&gt;
    /// </code>
    /// </example>
    public partial class RadzenDayView : SchedulerViewBase
    {
        /// <inheritdoc />
        public override string Icon => "calendar_view_day";

        /// <inheritdoc />
        [Parameter]
        public override string Text { get; set; } = "Day";

        /// <summary>
        /// Gets or sets the time format.
        /// </summary>
        /// <value>The time format. Set to <c>h tt</c> by default.</value>
        [Parameter]
        public string TimeFormat { get; set; } = "h tt";

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

        /// <inheritdoc />
        public override string Title
        {
            get
            {
                return Scheduler.CurrentDate.ToString(Scheduler.Culture.DateTimeFormat.ShortDatePattern);
            }
        }

        /// <inheritdoc />
        public override DateTime StartDate
        {
            get
            {
                return Scheduler.CurrentDate.Date.Add(StartTime);
            }
        }

        /// <inheritdoc />
        public override DateTime EndDate
        {
            get
            {
                return Scheduler.CurrentDate.Date.Add(EndTime);
            }
        }

        /// <inheritdoc />
        public override DateTime Next()
        {
            return Scheduler.CurrentDate.Date.AddDays(1);
        }

        /// <inheritdoc />
        public override DateTime Prev()
        {
            return Scheduler.CurrentDate.Date.AddDays(-1);
        }
    }
}
