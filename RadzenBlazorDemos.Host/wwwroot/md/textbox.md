# TextBox

The Blazor TextBox is a single-line text input with value binding, placeholder, max length, and read-only support.

Keywords: input, form, edit

> API reference: [RadzenTextBox API](https://blazor.radzen.com/api/textbox.md)

## Examples

## Blazor TextBox

The Blazor TextBox is a single-line text input with value binding, placeholder, max length, and read-only support.

### Get and Set the value of TextBox

Use `@-Value` to get the user input.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Default TextBox" Component="TextBoxBindValue" />
    <RadzenTextBox @bind-Value=@value Style="width: 100%; max-width: 400px;" Name="TextBoxBindValue" />
</RadzenStack>

<EventConsole @ref=@console />

@code {
    string value;

    EventConsole console;

    protected override void OnAfterRender(bool firstRender)
    {
        if (value != null)
        {
            console.Log($"Value changed to {value}");
        }
    }
}
```


### Placeholder

Use the `Placeholder` property to display hint text when the TextBox is empty.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Search" Component="TextBoxPlaceholder" />
    <RadzenTextBox Placeholder="Search..." Style="width: 100%; max-width: 400px;" Name="TextBoxPlaceholder" />
</RadzenStack>
```


### Maximum length

Use the `MaxLength` property to limit the number of characters the user can enter.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Max 5 characters" Component="TextBoxMaxLength" />
    <RadzenTextBox @bind-Value=@value MaxLength="5" Style="width: 100%; max-width: 400px;" Name="TextBoxMaxLength" />
</RadzenStack>

@code {
    string value;
}
```


### Change on every input

Set the `Immediate` property to raise the `Change` event on every keystroke.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Type something" Component="TextBoxImmediate" />
    <RadzenTextBox Immediate="true" Change=@(value => OnChange(value)) Style="width: 100%; max-width: 400px;" Name="TextBoxImmediate" />
</RadzenStack>

<EventConsole @ref=@console />

@code {
    EventConsole console;

    void OnChange(string value)
    {
        console.Log($"Value changed to {value}");
    }
}
```


### Disabled TextBox

Use the `Disabled` property to prevent user interaction.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Disabled" Component="TextBoxDisabled" />
    <RadzenTextBox Disabled="true" Value="Read only value" Style="width: 100%; max-width: 400px;" Name="TextBoxDisabled" />
</RadzenStack>
```


### AutoComplete

Use the `AutoComplete`, `autocomplete`, or `AutoCompleteType` properties to control browser autocomplete behavior.

```razor
<RadzenStack Gap="1rem" class="rz-p-sm-12">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Disabled AutoComplete" Style="width: 200px;" Component="TextBoxAutoCompleteDisabled" />
        <RadzenTextBox AutoComplete="false" Style="width: 100%; max-width: 400px;" Name="TextBoxAutoCompleteDisabled" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Custom AutoComplete" Style="width: 200px;" Component="TextBoxAutoCompleteCustom" />
        <RadzenTextBox autocomplete="custom" Style="width: 100%; max-width: 400px;" Name="TextBoxAutoCompleteCustom" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Predefined AutoCompleteType" Style="width: 200px;" Component="TextBoxAutoCompleteType" />
        <RadzenTextBox AutoCompleteType="AutoCompleteType.Sex" Style="width: 100%; max-width: 400px;" Name="TextBoxAutoCompleteType" />
    </RadzenStack>
</RadzenStack>
```


### TextBox Sizes

Use the `InputSize` property to set the TextBox size. Available sizes are ExtraSmall, Small, Medium (default), and Large.

```razor
<RadzenStack Gap="1rem" class="rz-p-sm-12">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Large" Style="width: 80px;" />
        <RadzenTextBox Placeholder="Large" InputSize="InputSize.Large" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Medium" Style="width: 80px;" />
        <RadzenTextBox Placeholder="Medium" InputSize="InputSize.Medium" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Small" Style="width: 80px;" />
        <RadzenTextBox Placeholder="Small" InputSize="InputSize.Small" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Extra Small" Style="width: 80px;" />
        <RadzenTextBox Placeholder="Extra Small" InputSize="InputSize.ExtraSmall" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
</RadzenStack>
```
