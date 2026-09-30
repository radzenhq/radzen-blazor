using Microsoft.AspNetCore.Components;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// A date range picker component that provides a calendar popup for selecting a start and end date.
    /// RadzenDateRangePicker binds to a <see cref="DateRange" /> value with nullable <see cref="DateRange.Start" /> and <see cref="DateRange.End" /> dates.
    /// Displays a text input rendering both dates separated by <see cref="Separator" /> and a calendar icon button. Clicking opens a popup calendar.
    /// The popup shows two months side by side by default - use <see cref="DisplayMonths" /> to change the number of displayed months.
    /// The first click in the calendar selects the start date, the second click selects the end date and closes the popup.
    /// Clicking a date before the pending start date restarts the selection from that date. While picking the end date the days
    /// between the start date and the hovered day are highlighted as a preview of the prospective range.
    /// Supports min/max date constraints, disabled dates via <see cref="RadzenDatePicker{TValue}.DateRender" />, inline calendar mode, and culture-specific formatting.
    /// </summary>
    /// <example>
    /// Basic date range picker:
    /// <code>
    /// &lt;RadzenDateRangePicker @bind-Value=@range /&gt;
    /// @code {
    ///     DateRange range = new DateRange(DateTime.Today, DateTime.Today.AddDays(7));
    /// }
    /// </code>
    /// Date range picker with constraints and custom format:
    /// <code>
    /// &lt;RadzenDateRangePicker @bind-Value=@range Min="@DateTime.Today" Max="@DateTime.Today.AddMonths(6)" DateFormat="dd/MM/yyyy" AllowClear="true" /&gt;
    /// </code>
    /// </example>
    public class RadzenDateRangePicker : RadzenDatePicker<DateRange>
    {
        internal override bool IsRange => true;

        internal override string RangeSeparatorText => Separator;

        internal override int MonthsToDisplay => Math.Clamp(DisplayMonths, 1, 6);

        /// <summary>
        /// Gets or sets the text separating the start and end dates in the input.
        /// </summary>
        /// <value>The separator text. Default is <c>" - "</c>.</value>
        [Parameter]
        public string Separator { get; set; } = " - ";

        /// <summary>
        /// Gets or sets the number of months displayed side by side in the calendar. Clamped between <c>1</c> and <c>6</c>.
        /// </summary>
        /// <value>The number of displayed months. Default is <c>2</c>.</value>
        [Parameter]
        public int DisplayMonths { get; set; } = 2;
    }
}
