# Switch

The Blazor Switch is a toggle switch that binds a bool value for on and off settings.

Keywords: form, edit, switch

> API reference: [RadzenSwitch API](https://blazor.radzen.com/api/switch.md)

## Examples

## Blazor Switch

The Blazor Switch is a toggle switch that binds a bool value for on and off settings.

### Get and set the value

As all Radzen Blazor input components the Switch has a `Value` property which gets and sets the value of the component. Use `@-Value` to get the user input.

```razor
<RadzenStack class="rz-p-sm-12">
    <RadzenSwitch @bind-Value=@value Change=@(args => OnChange(args, "Switch")) InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Switch value" }})" />
    <EventConsole @ref=@console Style="width: 100%;" />
</RadzenStack>

@code {
    bool value;

    EventConsole console;

    void OnChange(bool? value, string name)
    {
        console.Log($"{name} value changed to {value}");
    }
}
```


### Disabled Switch

To disable the switch, set `Disabled="true"`

```razor
<RadzenStack AlignItems="AlignItems.Center" class="rz-p-sm-12">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
        <RadzenLabel Text="True" Component="True" />
        <RadzenSwitch Value="true" Disabled="true" Name="True" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
        <RadzenLabel Text="False" Component="False"/>
        <RadzenSwitch Value="false" Disabled="true" Name="False" />
    </RadzenStack>
</RadzenStack>
```
