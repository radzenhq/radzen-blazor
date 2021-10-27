# Slider component
This article demonstrates how to use the Slider component. 

## Get and set the value
As all Radzen Blazor input components the Slider has a `Value` property which gets and sets the value of the component.
Use `@bind-Value` to get the user input. 

```
<RadzenSlider @bind-Value=@value TValue="int" Min="0" Max="100" Change=@OnChange />

@code {
    int value = 67;

    void OnChange(dynamic value)
    {
        var str = value is IEnumerable ? string.Join(", ", value) : value;
        console.Log($"{name} value changed to {str}");
    }
}
```

## Slider from -100 to 100
```
<RadzenSlider @bind-Value=@value TValue="int" Min="-100" Max="100" Change=@OnChange />

@code {
    int value = 67;

    void OnChange(dynamic value)
    {
        var str = value is IEnumerable ? string.Join(", ", value) : value;
        console.Log($"{name} value changed to {str}");
    }
}
```

## Slider with Step 10
```
<RadzenSlider @bind-Value=@value TValue="int" Step="10"  />

@code {
    void OnChange(dynamic value)
    {
        var str = value is IEnumerable ? string.Join(", ", value) : value;
        console.Log($"{name} value changed to {str}");
    }
}
```

## Range Slider
```
<RadzenSlider Range="true" @bind-Value=@values TValue="IEnumerable<int>"  />

@code {
    IEnumerable<int> values = new int[] { 14, 78 };

    void OnChange(dynamic value)
    {
        var str = value is IEnumerable ? string.Join(", ", value) : value;
        console.Log($"{name} value changed to {str}");
    }
}
```

