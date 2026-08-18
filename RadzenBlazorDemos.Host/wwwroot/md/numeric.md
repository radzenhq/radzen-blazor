# Numeric

The Blazor Numeric TextBox edits numbers with min/max limits, step buttons, formatted display, and culture-aware parsing.

Keywords: input, number, form, edit, numeric

> API reference: [RadzenNumeric API](https://blazor.radzen.com/api/numeric.md)

## Examples

## Blazor Numeric

The Blazor Numeric TextBox edits numbers with min/max limits, step buttons, formatted display, and culture-aware parsing.

### Get and Set the value of Numeric

As all Radzen Blazor input components the Numeric has a Value property which gets and sets the value of the component. Use `@-Value` to get the user input.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenNumeric @bind-Value=@value InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "enter value" }})" />
</div>

@code{
    int value;
}
```


### Get and Set the value of Numeric using Value and Change event

Value property can be used to set the value of the component and `Change` event to get the user input.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenNumeric TValue="int" Value=@value Change=@(args => value = args) InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "enter value" }})" />
</div>

@code{
    int value;
}
```


### Min set to 1 and Max set to 10

Use the `Min` and `Max` properties to constrain numeric input within a specific range.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenNumeric Min="1" Max="10" @bind-Value=@value InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "enter value" }})" />
</div>

@code{
    int value;
}
```


### Placeholder and 0.5 step

Use the `Placeholder` property to display hint text and `Step` to define increment/decrement intervals.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenNumeric Placeholder="0.0" Step="0.5" @bind-Value=@value InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "enter value" }})" />
</div>

@code{
    double? value;
}
```


### Without Up/Down

Use `ShowUpDown="false"` to hide the increment/decrement buttons and display only the text input.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenNumeric ShowUpDown="false" TValue="int?" @bind-Value=@value Placeholder="Enter or clear value" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "enter value" }})" />
</div>

@code{
    int? value;
}
```


### Formatted value

Use the `Format` property to apply culture-specific number formatting to the displayed value.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenNumeric TValue="double" Format="#.0000" @bind-Value=@value InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "enter value" }})" />
    <RadzenNumeric TValue="double" Format="c" @bind-Value=@value InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "enter value" }})" />
    <RadzenNumeric TValue="double" Format="### m2" @bind-Value=@value InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "enter value" }})" />

</div>

@code{
    double value = 0.0;
}
```


### Align value

Use the `InputAttributes` property to customize text alignment and other HTML input attributes.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenNumeric @bind-Value=@value TextAlign="TextAlign.Right" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "enter value" }})" />
</div>

@code{
    int value;
}
```


### Custom Value convert

Use the `ConvertValue` property to implement custom value conversion logic for specialized numeric types.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenNumeric @bind-Value=@value ConvertValue=@MyCustomValueConvert InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "enter value" }})" />
</div>

@code{
    decimal value;

    decimal MyCustomValueConvert(string value)
    {
        // Accept both comma and dot as decimal separator.
        var decimalSeparator = System.Globalization.CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator;
        return decimal.Parse(value.Replace(".", decimalSeparator).Replace(",", decimalSeparator));
    }
}
```


### Custom Numeric Type Support

Types that can be converted from a string, via a `TypeConverter` or assigning `ConvertValue`, get basic support from Numeric. If the type implements a TypeConverter that can convert to/from decimal, then `Step` and `Min`/`Max` are supported. If the type implements `IFormattable`, `Format` strings will be passed to it.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenStack Orientation="Orientation.Horizontal">
        <RadzenStack>
            <div>A <code>Temperature</code> type that implements <code>IFormattable</code>.  Value in Celsius: @value</div>
            <RadzenNumeric TValue="Temperature?" Format="F" @bind-Value=@value ConvertValue="ParseToTemperature" ShowUpDown="false"
                           InputAttributes="@(new Dictionary<string, object>() { { "aria-label", "enter value" } })"/>
        </RadzenStack>
        <RadzenStack>
            <div>A <code>Dollars</code> type that provides a <code>TypeConverter</code>, therefore <code>Min</code>, <code>Max</code>, and <code>Step</code> are supported.</div>
            <RadzenNumeric TValue="Dollars?" @bind-Value="@dollarValue" Min="1" Max="250" Step="5"
                           InputAttributes="@(new Dictionary<string, object>() { { "aria-label", "enter value" } })"/>
        </RadzenStack>
    </RadzenStack>
</div>

@code{
    Temperature? value = new(50.5m);
    
    Temperature? ParseToTemperature(string input)
    {
        return decimal.TryParse(input, out var val) ? new Temperature(val) : null;
    }

    Dollars? dollarValue = new Dollars(2.50m);
}
```


### Numeric Sizes

Use the `InputSize` property to set the Numeric size. Available sizes are ExtraSmall, Small, Medium (default), and Large.

```razor
<RadzenStack Gap="1rem" class="rz-p-sm-12" AlignItems="AlignItems.Center">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Style="width: 240px;">
        <RadzenLabel Text="Large" Style="width: 80px;" />
        <RadzenNumeric @bind-Value=@value TValue="decimal" InputSize="InputSize.Large" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Style="width: 240px;">
        <RadzenLabel Text="Medium" Style="width: 80px;" />
        <RadzenNumeric @bind-Value=@value TValue="decimal" InputSize="InputSize.Medium" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Style="width: 240px;">
        <RadzenLabel Text="Small" Style="width: 80px;" />
        <RadzenNumeric @bind-Value=@value TValue="decimal" InputSize="InputSize.Small" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Style="width: 240px;">
        <RadzenLabel Text="Extra Small" Style="width: 80px;" />
        <RadzenNumeric @bind-Value=@value TValue="decimal" InputSize="InputSize.ExtraSmall" />
    </RadzenStack>
</RadzenStack>

@code {
    decimal value = 100;
}
```
