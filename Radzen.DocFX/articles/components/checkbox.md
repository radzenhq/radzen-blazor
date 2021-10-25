# CheckBox component
This article demonstrates how to use the CheckBox component.

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