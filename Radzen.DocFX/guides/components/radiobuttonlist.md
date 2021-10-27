# RadioBoxList component
This article demonstrates how to use the RadioBoxList component.

## Get and set the value
As all Radzen Blazor input components the RadioBoxList has a `Value` property which gets and sets the value of the component.
Use `@bind-Value` to get the user input. The `TValue` property should be set to the `Value` property type.

## Data-binding
To display data in RadioBoxList component you can statically declare items in the markup and/or set collection of items (`IEnumerable<>`) to `Data` property, `TextProperty` to the string property name of the item in the collection and  `ValueProperty` to the property name with unique values in the collection.

### Statically declared items
```
<RadzenRadioBoxList @bind-Value=@value TValue="int" Change=@OnChange>
    <Items>
        <RadzenRadioBoxListItem Text="Orders" Value="1" />
        <RadzenRadioBoxListItem Text="Employees" Value="2" />
        <RadzenRadioBoxListItem Text="Customers" Value="3" />
    </Items>
</RadzenRadioBoxList>
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
<RadzenRadioBoxList Data="@data" TextProperty="Name" ValueProperty="Id" @bind-Value=@value TValue="int" Change=@OnChange />
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
<RadzenRadioBoxList Data="@data" TextProperty="Name" ValueProperty="Id" @bind-Value=@value TValue="int" Change=@OnChange>
    <Items>
        <RadzenRadioBoxListItem Text="Static item" Value="0" />
    </Items>
</RadzenRadioBoxList>
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
Use Orientation property to set if RadioBoxList orientation is horizontal or vertical.
```
<RadzenRadioButtonList Orientation="Orientation.Vertical" ...
```
