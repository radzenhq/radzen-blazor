# Mask component
This article demonstrates how to use the Mask component. 

## Get and set the value
As all Radzen Blazor input components the Mask has a `Value` property which gets and sets the value of the component.
Use `@bind-Value` to get the user input. Use `Mask`, `Pattern` and `Placeholder` properties to define and hint Mask component input.


## Telephone mask
```
<RadzenMask Mask="(***) ***-****" Pattern="[^0-9]" Placeholder="(000) 000-0000" Name="Phone" @bind-Value=@obj.Phone Change=@OnChange />

@code {
    public class MyObject
    {
        public string Phone { get; set; }
        public string CardNr { get; set; }
        public string SSN { get; set; }
    }

    MyObject obj = new MyObject();

    void OnChange(string value)
    {
        Console.WriteLine($"Value changed to {value}");
    }
}
```

## Credit Card mask
```
<RadzenMask Mask="**** **** **** ****" Pattern="[^0-9]" Placeholder="0000 0000 0000 0000" Name="CardNr" @bind-Value=@obj.CardNr Change=@OnChange />

@code {
    public class MyObject
    {
        public string Phone { get; set; }
        public string CardNr { get; set; }
        public string SSN { get; set; }
    }

    MyObject obj = new MyObject();

    void OnChange(string value)
    {
        Console.WriteLine($"Value changed to {value}");
    }
}
```

## SSN mask
```
<RadzenMask Mask="***-**-****" Pattern="[^0-9]" Placeholder="000-00-0000" Name="SSN" @bind-Value=@obj.SSN Change=@OnChange />

@code {
    public class MyObject
    {
        public string Phone { get; set; }
        public string CardNr { get; set; }
        public string SSN { get; set; }
    }

    MyObject obj = new MyObject();

    void OnChange(string value)
    {
        Console.WriteLine($"Value changed to {value}");
    }
}
```