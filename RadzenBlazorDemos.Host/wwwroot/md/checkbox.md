# CheckBox

The Blazor CheckBox binds a bool value with optional tri-state (true/false/null) support, plus disabled and read-only modes.

Keywords: form, edit

> API reference: [RadzenCheckBox API](https://blazor.radzen.com/api/checkbox.md)

## Examples

## Blazor CheckBox

The Blazor CheckBox binds a bool value with optional tri-state (true/false/null) support, plus disabled and read-only modes.

### Get and Set the value of CheckBox

As all Radzen Blazor input components the CheckBox has a Value property which gets and sets the value of the component. Use `@-Value` to get the user input.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenCheckBox @bind-Value=@value Name="CheckBox1" />
    <RadzenLabel Text="CheckBox" Component="CheckBox1" class="rz-ms-2" />
</div>

@code{
    bool value;
}
```


### Get and Set the value of CheckBox using Value and Change event

Value property can be used to set the value of the component and `Change` event to get the user input.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenCheckBox TValue="bool" Value=@value Change=@(args => value = args) Name="CheckBox2" />
    <RadzenLabel Text="CheckBox" Component="CheckBox2" class="rz-ms-2" />
</div>

@code{
    bool value;
}
```


### TriState CheckBox

Use `TriState="true"` to enable three states: checked, unchecked, and indeterminate.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenCheckBox TriState=true @bind-Value=@value Name="CheckBox3" />
    <RadzenLabel Text="CheckBox" Component="CheckBox3" class="rz-ms-2" />
</div>

@code{
    bool? value;
}
```


### Disabled CheckBox

Use `Disabled="true"` to disable the CheckBox and prevent user interaction.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenCheckBox Disabled=true @bind-Value=@value Name="CheckBox4" />
    <RadzenLabel Text="CheckBox" Component="CheckBox4" class="rz-ms-2" />
</div>

@code{
    bool value = true;
}
```


### ReadOnly CheckBox

Use `ReadOnly="true"` to make the CheckBox read-only, preventing changes while keeping it interactive.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenCheckBox ReadOnly=true @bind-Value=@value Name="CheckBox51" />
    <RadzenLabel Text="CheckBox" Component="CheckBox51" class="rz-ms-2" />
</div>

@code {
    bool value = true;
}
```
