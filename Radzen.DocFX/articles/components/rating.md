# Rating component
This article demonstrates how to use the Rating component. 

## Get and set the value
As all Radzen Blazor input components the Rating has a `Value` property which gets and sets the value of the component.
Use `@bind-Value` to get the user input. 

```
<RadzenRating @bind-Value=@value Stars="10" Change=@OnChange />
@code {
    int value = 3;
    
    void OnChange(int value)
    {
        console.Log($"Value changed to {value}");
    }
}
```