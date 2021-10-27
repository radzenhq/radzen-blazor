# Password component
This article demonstrates how to use the Password component. 

## Get and set the value
As all Radzen Blazor input components the Password has a `Value` property which gets and sets the value of the component.
Use `@bind-Value` to get the user input.

```
<RadzenPassword @bind-Value=@user.Password Change=@OnChange />
@code {
    ApplicationUser user;

    protected override async Task OnInitializedAsync()
    {
        user = // get the application user;
    }

    void OnChange(string value)
    {
        Console.WriteLine($"Value changed to {value}");
    }
}
```

## Properties
Use `Disabled`, `ReadOnly`, `Placeholder` and `AutoComplete` properties to control various HTML input type="password" attributes. You can set also arbitrary attributes not exposed as properties.
```
<RadzenPassword Placeholder="Type here .." MaxLength="10" AutoComplete="false" @oninput=@(args => OnChange(args.Value.ToString())) />

@code {
    void OnChange(string value)
    {
        Console.WriteLine($"Value changed to {value}");
    }
}
```