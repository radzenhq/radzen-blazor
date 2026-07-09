namespace Radzen;

/// <summary>
/// Specifies how users change the displayed month and year in the calendar of <see cref="Radzen.Blazor.RadzenDatePicker{TValue}" /> and <see cref="Radzen.Blazor.RadzenDateRangePicker" />.
/// </summary>
public enum DatePickerNavigationMode
{
    /// <summary>
    /// The calendar header displays month and year drop-downs. Calendars showing multiple months display static titles and navigation buttons only.
    /// </summary>
    DropDown,

    /// <summary>
    /// The calendar title is a button which drills up from the days view to a month grid and then to a year grid. Selecting a year or month drills back down.
    /// </summary>
    DrillDown
}
