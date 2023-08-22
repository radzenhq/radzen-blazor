# RadioButtonList component
This article demonstrates how to use the RadioButtonList component.

## Get and set the value
As all Radzen Blazor input components the RadioButtonList has a `Value` property which gets and sets the value of the component.
Use `@bind-Value` to get the user input. The `TValue` property should be set to the `Value` property type.

## Data-binding
To display data in RadioButtonList component you can statically declare items in the markup and/or set collection of items (`IEnumerable<>`) to `Data` property, `TextProperty` to the string property name of the item in the collection and  `ValueProperty` to the property name with unique values in the collection.

### Statically declared items
```
<RadzenRadioButtonList @bind-Value=@value TValue="int" Change=@OnChange>
    <Items>
        <RadzenRadioButtonListItem Text="Orders" Value="1" />
        <RadzenRadioButtonListItem Text="Employees" Value="2" />
        <RadzenRadioButtonListItem Text="Customers" Value="3" />
    </Items>
</RadzenRadioButtonList>
@code {
    int value = 1;

    void OnChange(int? value)
    {
        Console.WriteLine($"Value changed to {value}");
    }
}
```

### Items populated from data
```
<RadzenRadioButtonList Data="@data" TextProperty="Name" ValueProperty="Id" @bind-Value=@value TValue="int" Change=@OnChange />
@code {
    int value = 1;
    IEnumerable<MyObject> data = new MyObject[] {
        new MyObject(){ Id = 1 , Name = "Orders"}, new MyObject() { Id = 2 , Name = "Employees"}, new MyObject() { Id = 3 , Name = "Customers" } };

    void OnChange(int? value)
    {
        Console.WriteLine($"Value changed to {value}");
    }
}
```

### Statically declared and populated from data items
```
<RadzenRadioButtomList Data="@data" TextProperty="Name" ValueProperty="Id" @bind-Value=@value TValue="int" Change=@OnChange>
    <Items>
        <RadzenRadioButtonListItem Text="Static item" Value="0" />
    </Items>
</RadzenRadioButtonList>
@code {
    int value = 1;
    
    IEnumerable<MyObject> data = new MyObject[] {
        new MyObject(){ Id = 1 , Name = "Orders"}, new MyObject() { Id = 2 , Name = "Employees"}, new MyObject() { Id = 3 , Name = "Customers" } };

    void OnChange(int? value)
    {
        Console.WriteLine($"Value changed to {value}");
    }
}
```

### Orientation
Use Orientation property to set if RadioButtonList orientation is horizontal or vertical.
```
<RadzenRadioButtonList Orientation="Orientation.Vertical" ...
```
