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

        /// <inheritdoc />
        public override string Title
        {
            get
            {
                var culture = Scheduler?.Culture ?? CultureInfo.CurrentCulture;
                if (StartDate == EndDate.AddDays(-1))
                {
                    return $"{StartDate.ToString(culture.DateTimeFormat.ShortDatePattern, culture)}";
                }
                else
                {
                    return $"{StartDate.ToString(culture.DateTimeFormat.ShortDatePattern, culture)} - {EndDate.AddDays(-1).ToString(culture.DateTimeFormat.ShortDatePattern, culture)}";
                }
            }
        }

        /// <summary>
        /// Gets or sets number of days to view. Set to <c>2</c> by default.
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
