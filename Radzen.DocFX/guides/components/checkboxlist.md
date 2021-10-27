# CheckBoxList component
This article demonstrates how to use the CheckBoxList component.

## Get and set the value
As all Radzen Blazor input components the CheckBoxList has a `Value` property which gets and sets the value of the component.
Use `@bind-Value` to get the user input. The `TValue` property should be set to the `Value` property type.

## Data-binding
To display data in CheckBoxList component you can statically declare items in the markup and/or set collection of items (`IEnumerable<>`) to `Data` property, `TextProperty` to the string property name of the item in the collection and  `ValueProperty` to the property name with unique values in the collection.

### Statically declared items
```
<RadzenCheckBoxList @bind-Value=@values TValue="int" Change=@OnChange>
    <Items>
        <RadzenCheckBoxListItem Text="Orders" Value="1" />
        <RadzenCheckBoxListItem Text="Employees" Value="2" />
        <RadzenCheckBoxListItem Text="Customers" Value="3" />
    </Items>
</RadzenCheckBoxList>
@code {
    IEnumerable<int> values = new int[] { 1 };

    void OnChange(IEnumerable<int> value)
    {
        Console.WriteLine($"Value changed to {string.Join(", ", value)}");
    }
}
```

### Items populated from data
```
<RadzenCheckBoxList Data="@data" TextProperty="Name" ValueProperty="Id" @bind-Value=@values TValue="int" Change=@OnChange />
@code {
    IEnumerable<int> values = new int[] { 1 };
    IEnumerable<MyObject> data = new MyObject[] {
        new MyObject(){ Id = 1 , Name = "Orders"}, new MyObject() { Id = 2 , Name = "Employees"}, new MyObject() { Id = 3 , Name = "Customers" } };

    void OnChange(IEnumerable<int> value)
    {
        Console.WriteLine($"Value changed to {string.Join(", ", value)}");
    }
}
```

### Statically declared and populated from data items
```
<RadzenCheckBoxList Data="@data" TextProperty="Name" ValueProperty="Id" @bind-Value=@values TValue="int" Change=@OnChange>
    <Items>
        <RadzenCheckBoxListItem Text="Static item" Value="0" />
    </Items>
</RadzenCheckBoxList>
@code {
    IEnumerable<int> values = new int[] { 1 };
    
    IEnumerable<MyObject> data = new MyObject[] {
        new MyObject(){ Id = 1 , Name = "Orders"}, new MyObject() { Id = 2 , Name = "Employees"}, new MyObject() { Id = 3 , Name = "Customers" } };

    void OnChange(IEnumerable<int> value)
    {
        Console.WriteLine($"Value changed to {string.Join(", ", value)}");
    }
}
```

### Orientation
Use Orientation property to set if CheckBoxList orientation is horizontal or vertical.
```
<RadzenCheckBoxList Orientation="Orientation.Vertical" ...
```