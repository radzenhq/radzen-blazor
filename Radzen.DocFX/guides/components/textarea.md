# TextArea component
This article demonstrates how to use the TextArea component. 

## Get and set the value
As all Radzen Blazor input components the TextArea has a `Value` property which gets and sets the value of the component.
Use `@bind-Value` to get the user input.

```
<RadzenTextArea @bind-Value=@firstEmployee.FirstName TValue="string" Change=@OnChange />
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
Use `Disabled`, `ReadOnly`, `Placeholder`, `MaxLength`, `Rows`, `Cols` and `AutoComplete` properties to control various HTML textarea attributes. You can set also arbitrary attributes not exposed as properties.
```
<RadzenTextArea Placeholder="Type here .." MaxLength="100" Rows="2" Cols="2" AutoComplete="false" @oninput=@(args => OnChange(args.Value.ToString())) />

@code {
    void OnChange(string value)
    {
        Console.WriteLine($"Value changed to {value}");
    }
}
```