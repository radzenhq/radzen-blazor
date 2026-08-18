# TimeSpanPicker

Pick a duration or time span in the Blazor TimeSpanPicker, with inline mode, custom formatting, and min/max values.

Keywords: duration, form, edit, timespan

> API reference: [RadzenTimeSpanPicker API](https://blazor.radzen.com/api/timespanpicker.md)

## Examples

## Blazor TimeSpanPicker

Pick a duration or time span with the Blazor TimeSpanPicker - inline mode, custom formatting, and min/max values.

### Bind the value of TimeSpanPicker

As all Radzen Blazor input components, the TimeSpanPicker has a `Value` property which gets and sets the value of the component. Use `@-Value` to get the user input.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Time Span" Component="RadzenTimeSpanPickerBindValue"/>
    <RadzenTimeSpanPicker @bind-Value="@value" Name="RadzenTimeSpanPickerBindValue"/>
</RadzenStack>

@code {
    TimeSpan? value;
}
```


### Get and Set the value of TimeSpanPicker using Value and Change event.

`Value` property can be used to set the value of the component and `Change` event to get the user input.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Time Span" Component="RadzenTimeSpanPickerChangeEvent"/>
    <RadzenTimeSpanPicker Value="@value" Change="@(args => OnChange(args))" Name="RadzenTimeSpanPickerChangeEvent" />
</RadzenStack>

@code {
    TimeSpan? value;

    void OnChange(TimeSpan? newValue)
    {
        value = newValue;
    }
}
```


### Min and Max values

In this example, you can only set a value between 0 and 12h. Note that the + and - buttons are hidden because you cannot choose a negative value.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Time Span" Component="RadzenTimeSpanPickerMinMax"/>
    <RadzenTimeSpanPicker @bind-Value="@value" Name="RadzenTimeSpanPickerMinMax" Min="@TimeSpan.Zero" Max="@TimeSpan.FromHours(12)" />
</RadzenStack>

@code {
    TimeSpan? value;
}
```


### Inline picker

Use `Inline="true"` to display the time span picker as an always-visible control without a popup.

```razor
<RadzenStack Orientation="Orientation.Vertical" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenTimeSpanPicker @bind-Value="@value" Inline />
    <div>Current value: @value</div>
</RadzenStack>

@code {
    TimeSpan value = TimeSpan.Zero;
}
```


### Various configurations

Configure step increments, visible components, and other display options for the time span picker.

```razor
<RadzenFieldset AllowCollapse="false" Text="Field config">
    <RadzenStack Orientation="Orientation.Horizontal" Gap="1.5rem" Wrap="FlexWrap.Wrap">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
            <RadzenCheckBox @bind-Value="@allowClear" Name="AllowClear" />
            <RadzenLabel Text="Allow clear" Component="AllowClear" />
        </RadzenStack>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
            <RadzenCheckBox @bind-Value="@allowInput" Name="AllowInput" />
            <RadzenLabel Text="Allow input" Component="AllowInput" />
        </RadzenStack>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
            <RadzenCheckBox @bind-Value="@readOnly" Name="ReadOnly" />
            <RadzenLabel Text="Read only" Component="ReadOnly" />
        </RadzenStack>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
            <RadzenCheckBox @bind-Value="@disabled" Name="Disabled" />
            <RadzenLabel Text="Disabled" Component="Disabled" />
        </RadzenStack>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
            <RadzenCheckBox @bind-Value="@showPopupButton" Name="ShowPopupButton" />
            <RadzenLabel Text="Show popup button" Component="ShowPopupButton" />
        </RadzenStack>
    </RadzenStack>
</RadzenFieldset>
<RadzenFieldset AllowCollapse="false" Text="Panel config">
    <RadzenStack Orientation="Orientation.Horizontal" Gap="1.5rem" Wrap="FlexWrap.Wrap">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
            <RadzenLabel Text="Field precision" Component="FieldPrecision" />
            <RadzenDropDown Name="FieldPrecision" @bind-Value="@fieldPrecision" Data="@(Enum.GetValues<TimeSpanUnit>())" Style="width: 9rem;" />
        </RadzenStack>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
            <RadzenCheckBox @bind-Value="@padTimeValues" Name="PadTimeValues" />
            <RadzenLabel Text="Pad time values" Component="PadTimeValues" />
        </RadzenStack>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
            <RadzenCheckBox @bind-Value="@showConfirmationButton" Name="ShowConfirmationButton" />
            <RadzenLabel Text="Show confirmation button" Component="ShowConfirmationButton" />
        </RadzenStack>
    </RadzenStack>
</RadzenFieldset>
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Time Span" Component="RadzenTimeSpanPickerBoolConfig" />
    <RadzenTimeSpanPicker @bind-Value="@value" AllowClear="@allowClear" AllowInput="@allowInput" ReadOnly="@readOnly" Disabled="@disabled" ShowPopupButton="@showPopupButton" FieldPrecision="@fieldPrecision" PadTimeValues="@padTimeValues" ShowConfirmationButton="@showConfirmationButton" Name="RadzenTimeSpanPickerBoolConfig"  />
</RadzenStack>

@code {
    TimeSpan? value = new TimeSpan(1, 12, 30, 0);

    bool allowClear = true;
    bool allowInput = true;
    bool readOnly = false;
    bool disabled = false;
    bool showPopupButton = true;

    TimeSpanUnit fieldPrecision = TimeSpanUnit.Second;
    bool padTimeValues = false;
    bool showConfirmationButton = false;
}
```


### Time span format

Use the `Format` property to customize how the time span value is displayed in the input field.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Time Span" Component="RadzenTimeSpanPickerFormat"/>
    <RadzenTimeSpanPicker @bind-Value="@value" Name="RadzenTimeSpanPickerFormat" TimeSpanFormat="@timeSpanFormat" />
</RadzenStack>

@code {
    string timeSpanFormat => (value < TimeSpan.Zero ? "'-'" : "") + "d'd 'h'h 'm'min 's's'";
    TimeSpan? value = new TimeSpan(1, 12, 30, 0);
}
```


### Custom input parsing

The `ParseInput` parameter allows you to use a custom input parsing method. This way you can accept inputs like '30h 15min' or '-120s', or support more than one input format.

```razor
<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Time Span" Component="RadzenTimeSpanPickerParseInput"/>
    <RadzenTimeSpanPicker @bind-Value="@value" Name="RadzenTimeSpanPickerParseInput" ParseInput="@ParseTimeSpan" />
</RadzenStack>

@code {
    TimeSpan? value;

    string[] standardFormats = { "c", "g", "G" };
    Regex customTimeSpanRegex = new Regex(@"(?:(?<days>-?\d+)\w?d)|(?:(?<hours>-?\d+)\w?h)|(?:(?<minutes>-?\d+)\w?min)|(?:(?<seconds>-?\d+)\w?s)");

    public TimeSpan? ParseTimeSpan(string input)
    {
        foreach (var format in standardFormats)
        {
            if (TimeSpan.TryParseExact(input, format, null, System.Globalization.TimeSpanStyles.None, out var standardResult))
            {
                return standardResult;
            }
        }

        var regexGroups = customTimeSpanRegex.Matches(input.Trim())
            .Where(x => x.Success)
            .SelectMany(x => x.Groups.Cast<System.Text.RegularExpressions.Group>())
            .Where(x => x.Success)
            .ToArray();

        if (regexGroups.Length == 0)
        {
            return null;
        }

        var timeUnitToValue = new Dictionary<string, int>() {
            {"days", 0},
            {"hours", 0},
            {"minutes", 0},
            {"seconds", 0}
        };

        foreach (var timeUnitWithValue in timeUnitToValue)
        {
            var unit = timeUnitWithValue.Key;
            var valueString = regexGroups.FirstOrDefault(x => x.Name == unit)?.Value ?? "0";

            if (Int32.TryParse(valueString, out int value))
            {
                timeUnitToValue[unit] = value;
            }
        }

        var result = new TimeSpan(timeUnitToValue["days"], timeUnitToValue["hours"], timeUnitToValue["minutes"], timeUnitToValue["seconds"]);

        return result;
    }
}
```


### TimeSpanPicker Sizes

Use the `InputSize` property to set the TimeSpanPicker size. Available sizes are ExtraSmall, Small, Medium (default), and Large.

```razor
<RadzenStack Gap="1rem" class="rz-p-sm-12">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Large" Style="width: 80px;" />
        <RadzenTimeSpanPicker @bind-Value=@value InputSize="InputSize.Large" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Medium" Style="width: 80px;" />
        <RadzenTimeSpanPicker @bind-Value=@value InputSize="InputSize.Medium" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Small" Style="width: 80px;" />
        <RadzenTimeSpanPicker @bind-Value=@value InputSize="InputSize.Small" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Extra Small" Style="width: 80px;" />
        <RadzenTimeSpanPicker @bind-Value=@value InputSize="InputSize.ExtraSmall" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
</RadzenStack>

@code {
    TimeSpan? value;
}
```
