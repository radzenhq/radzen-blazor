# SelectBar component
This article demonstrates how to use the SelectBar component.

## Get and set the value
As all Radzen Blazor input components the SelectBar has a `Value` property which gets and sets the value of the component.
Use `@bind-Value` to get the user input. The `TValue` property should be set to the `Value` property type.

## Data-binding
To display data in SelectBar component you can statically declare items in the markup and/or set collection of items (`IEnumerable<>`) to `Data` property, `TextProperty` to the string property name of the item in the collection and  `ValueProperty` to the property name with unique values in the collection.

### Statically declared items with single selection
```
<RadzenSelectBar @bind-Value=@singleValue TValue="bool" Change=@OnChange>
    <Items>
        <RadzenSelectBarItem Text="On" Value="true" />
        <RadzenSelectBarItem Text="Off" Value="false" />
    </Items>
</RadzenSelectBar>
@code {
    bool singleValue = false;

    void OnChange(object value)
    {
        var str = value is IEnumerable<int> ? string.Join(", ", (IEnumerable<int>)value) : value;
        Console.WriteLine($"Value changed to {str}");
    }
}
```

### Statically declared items with multiple selection
```
<RadzenSelectBar @bind-Value=@values TValue="IEnumerable<int>" Multiple="true" Change=@OnChange>
    <Items>
        <RadzenSelectBarItem Text="Orders" Value="1" />
        <RadzenSelectBarItem Text="Employees" Value="2" />
        <RadzenSelectBarItem Text="Customers" Value="3" />
    </Items>
</RadzenSelectBar>
@code {
    IEnumerable<int> values = new int[] { 1, 2 };

    void OnChange(object value)
    {
        var str = value is IEnumerable<int> ? string.Join(", ", (IEnumerable<int>)value) : value;
        Console.WriteLine($"Value changed to {str}");
    }
}
```

### Items populated from data
```
<RadzenSelectBar @bind-Value=@values TValue="IEnumerable<int>" Multiple="true" Change=@OnChange Data="@data" TextProperty="Name" ValueProperty="Id" />
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
<RadzenSelectBar @bind-Value=@values Data="@data" TextProperty="Name" ValueProperty="Id" TValue="IEnumerable<int>" Multiple="true" Change=@OnChange>
    <Items>
        <RadzenSelectBarItem Text="Static item" Value="0" />
    </Items>
</RadzenSelectBar>
@code {
    IEnumerable<int> values = new int[] { 1, 2 };
    
    IEnumerable<MyObject> data = new MyObject[] {
        new MyObject(){ Id = 1 , Name = "Orders"}, new MyObject() { Id = 2 , Name = "Employees"}, new MyObject() { Id = 3 , Name = "Customers" } };

    void OnChange(object value)
    {
        var str = value is IEnumerable<int> ? string.Join(", ", (IEnumerable<int>)value) : value;
        Console.WriteLine($"Value changed to {str}");
    }
}
```