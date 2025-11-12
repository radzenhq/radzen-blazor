using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Globalization;

namespace Radzen.Blazor
{
    /// <summary>
    /// Displays the appointments in a week day in <see cref="RadzenScheduler{TItem}" />
    /// </summary>
    /// <code>
    /// &lt;RadzenScheduler Data="@appointments"&gt;
    ///     &lt;RadzenWeekView /&gt;
    /// &lt;/RadzenScheduler&gt;
    /// </code>
    public partial class RadzenWeekView : SchedulerViewBase
    {
        /// <inheritdoc />
        public override string Icon => "calendar_view_week";

        /// <inheritdoc />
        [Parameter]
        public override string Text { get; set; } = "Week";

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
        /// <inheritdoc />
        public override DateTime StartDate
        {
            get
            {
                var culture = Scheduler?.Culture ?? System.Globalization.CultureInfo.CurrentCulture;
                return Scheduler?.CurrentDate.Date.StartOfWeek(culture) ?? DateTime.Today.StartOfWeek(culture);
            }
        }

        /// <inheritdoc />
        public override DateTime EndDate
        {
            get
            {
                var culture = Scheduler?.Culture ?? System.Globalization.CultureInfo.CurrentCulture;
                return StartDate.EndOfWeek(culture).AddDays(1);
            }
        }

        /// <inheritdoc />
        public override string Title
        {
            get
            {
                var culture = Scheduler?.Culture ?? System.Globalization.CultureInfo.CurrentCulture;
                return $"{StartDate.ToString(culture.DateTimeFormat.ShortDatePattern, culture)} - {StartDate.EndOfWeek(culture).ToString(culture.DateTimeFormat.ShortDatePattern, culture)}";
            }
        }


        /// <inheritdoc />
        public override DateTime Next()
        {
            return Scheduler?.CurrentDate.Date.AddDays(7) ?? DateTime.Today.AddDays(7);
        }

        /// <inheritdoc />
        public override DateTime Prev()
        {
            return Scheduler?.CurrentDate.Date.AddDays(-7) ?? DateTime.Today.AddDays(-7);
        }
    }
}
