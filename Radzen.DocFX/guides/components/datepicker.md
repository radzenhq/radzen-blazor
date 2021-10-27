# DatePicker component
This article demonstrates how to use the DatePicker component.

## Get and set the value
As all Radzen Blazor input components the DatePicker has a `Value` property which gets and sets the value of the component.
Use `@bind-Value` to get the user input.

```
<RadzenDatePicker @bind-Value=@value Change=@OnChange />
@code {
    DateTime? value = DateTime.Today;

    void OnChange(DateTime? value)
    {
        Console.WriteLine($"Value changed to {value}");
    }
}
```

## Use DateFormat property to specify format of the DateTime value displayed in the component input.
```
<RadzenDatePicker @bind-Value=@value DateFormat="d" />
```

## Use ShowTime, ShowSeconds, ShowTimeOkButton and TimeOnly properties to control the time parts visibility of the DatePicker.
```
<RadzenDatePicker @bind-Value=@value ShowTime="true" ShowSeconds="true" ShowTimeOkButton="true" TimeOnly="true" />
```

## Use HoursStep, MinutesStep and SecondsStep properties to set step for the time numeric inputs of the DatePicker.
```
<RadzenDatePicker @bind-Value=@value HoursStep="1.5" MinutesStep="5" SecondsStep="10"  />
```

## Use Inline="true" to display just the calendar component of the DatePicker.
```
<RadzenDatePicker @bind-Value=@value Inline="true" />
```

## Use DateRender event to set attributes for the date.
```
<RadzenDatePicker @bind-Value=@value DateRender=@DateRenderSpecial />
@code {
    DateTime? value = DateTime.Today;
    IEnumerable<DateTime> dates = new DateTime[] { DateTime.Today.AddDays(-1), DateTime.Today.AddDays(1) };

    void DateRenderSpecial(DateRenderEventArgs args)
    {
        if (dates.Contains(args.Date))
        {
            args.Attributes.Add("style", "background-color: #ff6d41; border-color: white;");
        }

        args.Disabled = dates.Contains(args.Date);
    }
}
```