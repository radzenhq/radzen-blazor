using Microsoft.AspNetCore.Components;
using System;
using System.Globalization;

namespace Radzen.Blazor
{
    /// <summary>
    /// Displays the appointments as an agenda <see cref="RadzenScheduler{TItem}" />
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenScheduler Data="@appointments"&gt;
    ///     &lt;RadzenAgendaView /&gt;
    /// &lt;/RadzenScheduler&gt;
    /// </code>
    /// </example>
    public partial class RadzenAgendaView : SchedulerViewBase
    {
        /// <inheritdoc />
        public override string Icon => "lists";

        /// <inheritdoc />
        [Parameter]
        public override string Text { get; set; } = "Agenda";

        /// <summary>
        /// Gets or sets the time format.
        /// </summary>
        /// <value>The time format. Set to <c>h tt</c> by default.</value>
        [Parameter]
        public string TimeFormat { get; set; } = "h tt";

        private string? multiDayText;

        /// <summary>
        /// Gets or sets the text displayed for appointments that span more than one day. Set to <c>Multi-day</c> by default.
        /// </summary>
        /// <value>The multi-day text.</value>
        [Parameter]
        public string MultiDayText { get => multiDayText ?? Localize(nameof(RadzenStrings.AgendaView_MultiDayText)); set => multiDayText = value; }

        /// <summary>
        /// Gets or sets a value indicating whether to show the time display for appointments.
        /// </summary>
        /// <value><c>true</c> if the time display should be shown; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowTimeDisplay { get; set; } = true;

        /// <inheritdoc />
        public override string Title
        {
            get
            {
                var culture = Scheduler?.Culture ?? CultureInfo.CurrentCulture;
                var end = EndDate.AddDays(-1);
                var title = StartDate == end
                    ? $"{StartDate.ToString(culture.DateTimeFormat.ShortDatePattern, culture)}"
                    : $"{StartDate.ToString(culture.DateTimeFormat.ShortDatePattern, culture)} - {end.ToString(culture.DateTimeFormat.ShortDatePattern, culture)}";
                return FormatTitle(StartDate, end, title);
            }
        }

        /// <summary>
        /// Gets or sets number of days to view. Set to <c>1</c> by default.
        /// </summary>
        /// <value>The number of days.</value>
        [Parameter]
        public int NumberOfDays { get; set; } = 1;

        /// <inheritdoc />
        public override DateTime StartDate
        {
            get
            {
                return Scheduler?.CurrentDate.Date ?? DateTime.Today;
            }
        }

        /// <inheritdoc />
        public override DateTime EndDate
        {
            get
            {
                return Scheduler?.CurrentDate.Date.AddDays(NumberOfDays) ?? DateTime.Today.AddDays(NumberOfDays);
            }
        }

        /// <inheritdoc />
        public override DateTime Next()
        {
            return Scheduler?.CurrentDate.Date.AddDays(NumberOfDays) ?? DateTime.Today.AddDays(NumberOfDays);
        }

        /// <inheritdoc />
        public override DateTime Prev()
        {
            return Scheduler?.CurrentDate.Date.AddDays(-NumberOfDays) ?? DateTime.Today.AddDays(-NumberOfDays);
        }
    }
}
