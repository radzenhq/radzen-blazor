# Numeric component
This article demonstrates how to use the Numeric component. 

## Get and set the value
As all Radzen Blazor input components the Numeric has a `Value` property which gets and sets the value of the component.
Use `@bind-Value` to get the user input. 


## Min and Max
```
<RadzenNumeric TValue="int" Min="1" Max="10" Change=@OnChange />

@code {
    void OnChange(int value)
    {
        Console.WriteLine($"Value changed to {value}");
    }
}
```

## Step
```
<RadzenNumeric TValue="double" Placeholder="0.0" Step="0.5" Change=@OnChange />

@code {
    void OnChange(double value)
    {
        Console.WriteLine($"Value changed to {value}");
    }
}
```

## Formatted Value
```
<RadzenNumeric TValue="double" Format="0.0000" @bind-Value=@dblValue Placeholder="Enter or clear value" Change="@OnChange />

@code {
    double dblValue = 0.0;

    void OnChange(double value)
    {
        Console.WriteLine($"Value changed to {value}");
    }
}
```

## Without Up/Down buttons
```
<RadzenNumeric ShowUpDown="false" TValue="int?" @bind-Value=@value Placeholder="Enter or clear value" Change="@OnChange />

@code {
    int? value = null;
    
    void OnChange(int? value)
    {
        Console.WriteLine($"Value changed to {value}");
    }
}
```