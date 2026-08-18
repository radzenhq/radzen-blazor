# DatePicker

The Radzen Blazor DatePicker is a date and time picker with an inline calendar mode, time selection, date ranges, min/max and disabled dates, and DateOnly/TimeOnly binding.

Keywords: calendar, time, form, edit, datepicker

> API reference: [RadzenDatePicker API](https://blazor.radzen.com/api/datepicker.md)

## Examples

## Blazor DatePicker

The Radzen Blazor DatePicker is a date and time picker with an inline calendar mode, time selection, date ranges, min/max and disabled dates, and DateOnly/TimeOnly binding.

### Get and Set the value of DatePicker

As all Radzen Blazor input components the DatePicker has a Value property which gets and sets the value of the component. Use `@-Value` to get the user input.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Date" Component="RadzenDatePickerBindValue" />
    <RadzenDatePicker @bind-Value=@value Name="RadzenDatePickerBindValue" ShowCalendarWeek />
</RadzenStack>

@code {
    DateTime? value;
}
```


### DatePicker with immediate value update

Use `Immediate="true"` to update the bound value as the user types a valid date, rather than waiting for the input to lose focus.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Date" Component="RadzenDatePickerImmediate" />
    <RadzenDatePicker @bind-Value=@value Immediate="true" Change="@(_ => OnChange())" Name="RadzenDatePickerImmediate" />
</RadzenStack>

<EventConsole @ref=@console />

@code {
    DateTime? value;
    EventConsole console;

    void OnChange()
    {
        console.Log($"Value changed to {value}");
    }
}
```


### Get and Set the value of DatePicker using Value and Change event

Value property can be used to set the value of the component and `Change` event to get the user input.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Date" Component="RadzenDatePickerChangeEvent" />
    <RadzenDatePicker TValue="DateTime?" Value=@value Change="@(args => value = args)" Name="RadzenDatePickerChangeEvent" />
</RadzenStack>

@code {
    DateTime? value = DateTime.Now;
}
```


### DatePicker with time

Use `ShowTime="true"` to enable time selection alongside date selection.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Date" Component="DatePickerWithTime" />
    <RadzenDatePicker @bind-Value=@value ShowTime="true" ShowSeconds="true" HoursStep="1.5" MinutesStep="5" SecondsStep="10" DateFormat="MM/dd/yyyy HH:mm" Name="DatePickerWithTime" />
</RadzenStack>

@code {
    DateTime? value = DateTime.Now;
}
```


### Define hour format

Use the `HoursStep`, `MinutesStep`, and `SecondsStep` properties to configure time picker increments.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Date" Component="DatePickerHourFormat" />
    <RadzenDatePicker @bind-Value=@value ShowTime="true" HourFormat="12" DateFormat="MM/dd/yyyy h:mm tt" Name="DatePickerHourFormat" />
</RadzenStack>

@code {
    DateTime? value = DateTime.Now;
}
```


### Time-only DatePicker

Use `TimeOnly="true"` to display only the time picker without the date calendar.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Time" Component="DatePickerTimeOnly" />
    <RadzenDatePicker @bind-Value=@value ShowTime="true" TimeOnly="true" DateFormat="HH:mm" Name="DatePickerTimeOnly" />
</RadzenStack>

@code {
    DateTime? value = DateTime.Now;
}
```


### DatePicker with special or disabled dates

Use the `DateAttributes` property to highlight specific dates or disable certain dates from selection.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Date" Component="DatePickerSpecialDates" />
    <RadzenDatePicker @bind-Value=@value DateRender=@DateRender Name="DatePickerSpecialDates" />
</RadzenStack>

@code {
    DateTime? value = DateTime.Now;
    IEnumerable<DateTime> dates = new DateTime[] { DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1) };

    void DateRender(DateRenderEventArgs args)
    {
        var special = dates.Select(d => d.Date).Contains(args.Date.Date);
        if (special)
        {
            args.Attributes.Add("style", "background-color: #ff6d41; border-color: white;");
        }

        args.Disabled = special || args.Disabled || args.Date.DayOfWeek == DayOfWeek.Sunday || args.Date.DayOfWeek == DayOfWeek.Saturday;
    }
}
```


### DatePicker with initial view date and year range

Use `InitialViewDate`, `YearRange`, and `InitialView` properties to control the calendar's starting view.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Date" Component="DatePickerInitialViewDate" />
    <RadzenDatePicker TValue="DateTime?" InitialViewDate="new DateTime(2040,06,01)" YearRange="1900:2100" Name="DatePickerInitialViewDate" />
</RadzenStack>
```


### Set Min and Max dates

Use the `Min` and `Max` properties to restrict date selection to a specific range.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Date" Component="DatePickerMinMax" />
    <RadzenDatePicker @bind-Value=@value Min="DateTime.Today.AddDays(-7)" Max="DateTime.Today.AddDays(7)" Name="DatePickerMinMax" />
</RadzenStack>

@code {
    DateTime? value = DateTime.Now;
}
```


### DatePicker with no button

Use `ShowButton="false"` to hide the calendar icon button and open the picker by clicking the input field.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Date" Component="DatePickerNoButton" />
    <RadzenDatePicker @bind-Value=@value Name="DatePickerNoButton" AllowClear="true" ShowButton="false" />
</RadzenStack>

@code {
    DateOnly? value;
}
```


### DatePicker with no input box

Hide the input field to display only the calendar button for date selection.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Date" Component="DatePickerNoInputBox" />
    <RadzenDatePicker @bind-Value=@value Name="DatePickerNoInputBox" AllowClear="true" ShowInput="false" />
</RadzenStack>
<RadzenText TextAlign="TextAlign.Center">You selected <strong>@(value == null ? "(no date)" : value.Value.ToShortDateString())</strong>.</RadzenText>

@code {
    DateTime? value;
}
```


### DatePicker with custom footer

Use the `FooterTemplate` property to add custom content at the bottom of the date picker calendar.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Date" Component="DatePickerFooterTemplate" />
    <RadzenDatePicker @bind-Value=@value Name="DatePickerFooterTemplate">
        <FooterTemplate>
            <RadzenButton Click=@(args => value = DateTime.Now) Text="Today" Style="width: 100%;" class="rz-my-4" />
        </FooterTemplate>
    </RadzenDatePicker>
</RadzenStack>

@code {
    DateTime? value = DateTime.Now;
}
```


### DatePicker with custom input parsing

The Radzen Blazor DatePicker has a parameter named `ParseInput` which allows for a fully custom parse-method. This way you can accept inputs like '3012' or '30122023' and support more than one input-format. Click on the 'Edit Source' to see the implementation.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Date" Component="DatePickerParseInput" />
    <RadzenDatePicker @bind-Value=@value ParseInput="@ParseDate" Name="DatePickerParseInput" />
</RadzenStack>

@code {
    DateTime? value = DateTime.Now;

    public DateTime? ParseDate(string input)
    {
        string[] formats = { "dd-MM-yyyy", "dd/MM/yyyy", "dd-MM-yy", "dd/MM/yy", "ddMMyyyy", "ddMMyy", "dd-MM", "dd/MM", "ddMM" };

        foreach (var format in formats)
        {
            if (DateTime.TryParseExact(input, format, null, System.Globalization.DateTimeStyles.None, out var result))
            {
                return result;
            }
        }

        return null;
    }
}
```


### DatePicker as calendar

Use `Inline="true"` to display the DatePicker as an always-visible calendar without an input field.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenDatePicker @bind-Value=@value Inline="true" />
</div>

@code {
    DateTime? value = DateTime.Now;
}
```


### Multiple dates selection

Use `Multiple="true"` to enable selection of multiple dates in the calendar.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Dates" Component="DatePickerMultiple" />
    <RadzenDatePicker Multiple @bind-Value=@values DateFormat="dd/MM/yyyy" Name="DatePickerMultiple" />
</RadzenStack>

@code {
    IEnumerable<DateTime?> values;
}
```


### DatePicker for year/month selection

Configure the DatePicker to select only years or months by setting the appropriate view mode.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Year/Month" Component="DatePickerYearMonth" />
    <RadzenDatePicker @bind-Value=@value ShowDays=false DateFormat="yyyy/MM" CurrentDateChanged=@OnCurrentDateChanged Name="DatePickerYearMonth" />
</RadzenStack>

@code {
    DateTime value = DateTime.Now;

    void OnCurrentDateChanged(DateTime args)
    {
        value = new DateTime(args.Year, args.Month, 1);
    }
}
```


### DatePicker binds to types DateOnly or TimeOnly

`Value` property can be bound to values of type `DateOnly` or `TimeOnly`

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Wrap="FlexWrap.Wrap" class="rz-p-12">
    <RadzenStack Gap="0.5rem">
        <RadzenLabel Text="Select Date, bound to DateOnly" Component="DatePickerDateOnlyType" />
        <RadzenDatePicker @bind-Value="@value" DateFormat="MM/dd/yyyy" Name="DatePickerDateOnlyType"/>
    </RadzenStack>
    <RadzenStack Gap="0.5rem">
        <RadzenLabel Text="Select Time, bound to TimeOnly" Component="DatePickerTimeOnlyType" />
        <RadzenDatePicker @bind-Value="@timeValue" ShowSeconds="true" DateFormat="HH:mm" Name="DatePickerTimeOnlyType" />
    </RadzenStack>
</RadzenStack>

@code {
    DateOnly value = DateOnly.FromDateTime(DateTime.Now);
    TimeOnly timeValue = TimeOnly.FromDateTime(DateTime.Now);
}
```


### DatePicker Sizes

Use the `InputSize` property to set the DatePicker size. Available sizes are ExtraSmall, Small, Medium (default), and Large.

```razor
<RadzenStack Gap="1rem" class="rz-p-sm-12">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Large" Style="width: 80px;" />
        <RadzenDatePicker @bind-Value=@value InputSize="InputSize.Large" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Medium" Style="width: 80px;" />
        <RadzenDatePicker @bind-Value=@value InputSize="InputSize.Medium" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Small" Style="width: 80px;" />
        <RadzenDatePicker @bind-Value=@value InputSize="InputSize.Small" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Extra Small" Style="width: 80px;" />
        <RadzenDatePicker @bind-Value=@value InputSize="InputSize.ExtraSmall" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
</RadzenStack>

@code {
    DateTime? value = DateTime.Today;
}
```
