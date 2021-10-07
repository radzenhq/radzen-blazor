using Microsoft.AspNetCore.Components;
using Radzen.Blazor.Rendering;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenMonthView.
    /// Implements the <see cref="Radzen.Blazor.SchedulerViewBase" />
    /// </summary>
    /// <seealso cref="Radzen.Blazor.SchedulerViewBase" />
    public partial class RadzenMonthView : SchedulerViewBase
    {
        /// <summary>
        /// Gets the title.
        /// </summary>
        /// <value>The title.</value>
        public override string Title
        {
            get => Scheduler.CurrentDate.ToString("MMMM yyyy", Scheduler.Culture);
        }

        /// <summary>
        /// Gets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public override string Text { get; set; } = "Month";

        /// <summary>
        /// Gets or sets the maximum appointments in slot.
        /// </summary>
        /// <value>The maximum appointments in slot.</value>
        [Parameter]
        public int? MaxAppointmentsInSlot { get; set; }

        /// <summary>
        /// Gets or sets the more text.
        /// </summary>
        /// <value>The more text.</value>
        [Parameter]
        public string MoreText { get; set; } = "+ {0} more";

        /// <summary>
        /// Gets the start date.
        /// </summary>
        /// <value>The start date.</value>
        public override DateTime StartDate
        {
            get
            {
                return Scheduler.CurrentDate.Date.StartOfMonth().StartOfWeek();
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
                return Scheduler.CurrentDate.Date.EndOfMonth().EndOfWeek().AddDays(1);
            }
        }

        /// <summary>
        /// Nexts this instance.
        /// </summary>
        /// <returns>DateTime.</returns>
        public override DateTime Next()
        {
            return Scheduler.CurrentDate.Date.StartOfMonth().AddMonths(1);
        }

        /// <summary>
        /// Previouses this instance.
        /// </summary>
        /// <returns>DateTime.</returns>
        public override DateTime Prev()
        {
            return Scheduler.CurrentDate.Date.StartOfMonth().AddMonths(-1);
        }
    }
}