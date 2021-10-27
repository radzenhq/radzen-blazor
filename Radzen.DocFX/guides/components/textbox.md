# TextBox component
This article demonstrates how to use the TextBox component. 

## Get and set the value
As all Radzen Blazor input components the TextBox has a `Value` property which gets and sets the value of the component.
Use `@bind-Value` to get the user input.

```
<RadzenTextBox @bind-Value=@firstEmployee.FirstName TValue="string" Change=@OnChange />
@code {
    Employee firstEmployee;

    protected override async Task OnInitializedAsync()
    {
        firstEmployee = await Task.FromResult(dbContext.Employees.FirstOrDefault());
    }

    void OnChange(string value)
    {
        Console.WriteLine($"Value changed to {value}");
    }
}
```

## Properties
Use `Disabled`, `ReadOnly`, `Placeholder`, `MaxLength` and `AutoComplete` properties to control various HTML input attributes. You can set also arbitrary attributes not exposed as properties.
```
<RadzenTextBox Placeholder="Type here .." MaxLength="10" AutoComplete="false" @oninput=@(args => OnChange(args.Value.ToString())) />

@code {
    void OnChange(string value)
    {
        Console.WriteLine($"Value changed to {value}");
    }
}
```