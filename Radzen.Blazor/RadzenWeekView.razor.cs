using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;
using System.Globalization;
using System.Threading.Tasks;

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

        /// <summary>
        /// Gets or sets a value indicating whether a dedicated all-day row is displayed when the view groups appointments by resource.
        /// All-day appointments are displayed in it instead of the time grid. Set to <c>true</c> by default.
        /// </summary>
        /// <value><c>true</c> if the all-day row is visible; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool ShowAllDay { get; set; } = true;

        private string? allDayText;

        /// <summary>
        /// Gets or sets the text displayed in the header of the all-day row when the view groups appointments by resource. Set to <c>All day</c> by default.
        /// </summary>
        /// <value>The all-day text.</value>
        [Parameter]
        public string AllDayText
        {
            get => allDayText ?? Localize(nameof(RadzenStrings.Scheduler_AllDayText));
            set => allDayText = value;
        }

        /// <summary>
        /// Called by the Blazor runtime when parameters are set.
        /// </summary>
        /// <param name="parameters">The parameters.</param>
        public override async Task SetParametersAsync(ParameterView parameters)
        {
            var allDayChanged = parameters.DidParameterChange(nameof(ShowAllDay), ShowAllDay) ||
                parameters.DidParameterChange(nameof(AllDayText), AllDayText);

            await base.SetParametersAsync(parameters);

            if (allDayChanged && Scheduler != null)
            {
                await Scheduler.Reload();
            }
        }

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
