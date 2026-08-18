# Password

The Blazor Password TextBox masks input, with autocomplete control and placeholder support.

Keywords: input, form, edit

> API reference: [RadzenPassword API](https://blazor.radzen.com/api/password.md)

## Examples

## Blazor Password

The Blazor Password TextBox masks input, with autocomplete control and placeholder support.

### Get and Set the value of Password

As all Radzen Blazor input components the Password has a Value property which gets and sets the value of the component. Use `@-Value` to get the user input.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenPassword @bind-Value=@value aria-label="enter password" />
</div>

@code{
    string value;
}
```


### Get and Set the value of Password using Value and Change event

Value property can be used to set the value of the component and `Change` event to get the user input.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenPassword Value=@value Change=@(args => value = args) aria-label="enter password" />
</div>

@code{
    string value;
}
```


### Define placeholder

Use the `Placeholder` property to display hint text when the password field is empty.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenPassword @bind-Value=@value Placeholder="Enter password..." aria-label="enter password" />
</div>

@code{
    string value;
}
```


### Without auto-complete

Disable browser password auto-completion for enhanced security in specific scenarios.

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenPassword @bind-Value=@value Placeholder="Enter password..." AutoComplete="false" aria-label="enter password" />
</div>

@code{
    string value;
}
```


### With Immediate

push new value on each keystroke

```razor
<div class="rz-p-12 rz-text-align-center">
    <RadzenPassword @bind-Value=@value Placeholder="Enter password..." Immediate="true" Change="@(_ => OnChange())" aria-label="enter password" />
</div>

<EventConsole @ref=@console />

@code{
    string value;
    EventConsole console;

    void OnChange()
    {
	    console.Log($"password was changed");
    }
}
```


### Password Sizes

Use the `InputSize` property to set the Password size. Available sizes are ExtraSmall, Small, Medium (default), and Large.

```razor
<RadzenStack Gap="1rem" class="rz-p-sm-12">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Large" Style="width: 80px;" />
        <RadzenPassword Placeholder="Enter password..." InputSize="InputSize.Large" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Medium" Style="width: 80px;" />
        <RadzenPassword Placeholder="Enter password..." InputSize="InputSize.Medium" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Small" Style="width: 80px;" />
        <RadzenPassword Placeholder="Enter password..." InputSize="InputSize.Small" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Extra Small" Style="width: 80px;" />
        <RadzenPassword Placeholder="Enter password..." InputSize="InputSize.ExtraSmall" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
</RadzenStack>
```
