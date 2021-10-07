using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenWeekView.
    /// Implements the <see cref="Radzen.Blazor.SchedulerViewBase" />
    /// </summary>
    /// <seealso cref="Radzen.Blazor.SchedulerViewBase" />
    public partial class RadzenWeekView : SchedulerViewBase
    {
        /// <summary>
        /// Gets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public override string Text { get; set; } = "Week";

        /// <summary>
        /// Gets or sets the time format.
        /// </summary>
        /// <value>The time format.</value>
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
        /// Gets the start date.
        /// </summary>
        /// <value>The start date.</value>
        public override DateTime StartDate
        {
            get
            {
                return Scheduler.CurrentDate.Date.StartOfWeek();
            }
        }

        /// <summary>
        /// Gets the end date.
        /// </summary>
        /// <value>The end date.</value>
        public override DateTime EndDate
        {
            get
            {
                return StartDate.EndOfWeek().AddDays(1);
            }
        }

        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <value>The title.</value>
        public override string Title
        {
            get
            {
                return $"{StartDate.ToString(Scheduler.Culture.DateTimeFormat.ShortDatePattern)} - {StartDate.EndOfWeek().ToString(Scheduler.Culture.DateTimeFormat.ShortDatePattern)}";
            }
        }

        /// <summary>
        /// Nexts this instance.
        /// </summary>
        /// <returns>DateTime.</returns>
        public override DateTime Next()
        {
            return Scheduler.CurrentDate.Date.AddDays(7);
        }

        /// <summary>
        /// Previouses this instance.
        /// </summary>
        /// <returns>DateTime.</returns>
        public override DateTime Prev()
        {
            return Scheduler.CurrentDate.Date.AddDays(-7);
        }
    }
}