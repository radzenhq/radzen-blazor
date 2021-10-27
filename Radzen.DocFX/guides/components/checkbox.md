# CheckBox component
This article demonstrates how to use the CheckBox component.

## Get and set the value
As all Radzen Blazor input components the CheckBox has a `Value` property which gets and sets the value of the component.
Use `@bind-Value` to get the user input.

```
<RadzenCheckBox @bind-Value=@checkBoxValue TriState="true" TValue="bool?" Change=@OnChange />
@code {
    bool? checkBoxValue;

    void OnChange(bool? value)
    {
        Console.WriteLine($"Value changed to {value}");
    }
}
```
