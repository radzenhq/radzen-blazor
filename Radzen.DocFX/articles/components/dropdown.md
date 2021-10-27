# DropDown component
This article demonstrates how to use the DropDown component.
 
## Get and set the value
As all Radzen Blazor input components the CheckBoxList has a `Value` property which gets and sets the value of the component.
Use `@bind-Value` to get the user input. The `TValue` property should be set to the `Value` property type.

## Data-binding
To display data in DropDown component you need to set collection of items (`IEnumerable<>`) to `Data` property. Optionally you can set `TextProperty` to the string property name of the item in the collection and  `ValueProperty` to the property name with unique values in the collection.

### Binding to simple collection

```
<RadzenDropDown TValue="string" Data=@(customers.Select(c => c.CompanyName).Distinct()) Change="@OnChange" />

@code {
    IEnumerable<Customer> customers;

    protected override void OnInitialized()
    {
        customers = dbContext.Customers.ToList();
    }

    void OnChange(object value)
    {
        var str = value is IEnumerable<object> ? string.Join(", ", (IEnumerable<object>)value) : value;

        Console.WriteLine($"Value changed to {str}");
    }
}
```

### Binding to list of objects

```
<RadzenDropDown TValue="string" Data=@customers TextProperty="CompanyName" ValueProperty="CustomerID" Change="@OnChange" />

@code {
    IEnumerable<Customer> customers;

    protected override void OnInitialized()
    {
        customers = dbContext.Customers.ToList();
    }

    void OnChange(object value)
    {
        var str = value is IEnumerable<object> ? string.Join(", ", (IEnumerable<object>)value) : value;

        Console.WriteLine($"Value changed to {str}");
    }
}
```

### Binding using LoadData event with filtering.

```
<RadzenDropDown TValue="string" Data=@customers TextProperty="CompanyName" ValueProperty="CustomerID" LoadData=@LoadData AllowFiltering="true" />

@code {
    IEnumerable<Customer> customers;

    void LoadData(LoadDataArgs args)
    {
        var query = dbContext.Customers.AsQueryable();

        if (!string.IsNullOrEmpty(args.Filter))
        {
            query = query.Where(c => c.CustomerID.ToLower().Contains(args.Filter.ToLower()) || c.ContactName.ToLower().Contains(args.Filter.ToLower()));
        }

        customers = query.ToList();

        InvokeAsync(StateHasChanged);
    }
}
```

### Virtualization using IQueryable.

```
<RadzenDropDown TValue="string" AllowVirtualization="true" AllowFiltering="true" Data=@customers TextProperty="CompanyName" ValueProperty="CustomerID" />

@code {
    IEnumerable<Customer> customers;

    protected override void OnInitialized()
    {
        customers = dbContext.Customers.ToList();
    }
}
```

### Virtualization using LoadData event.

```
<RadzenDropDown TValue="string" AllowVirtualization="true" LoadData=@LoadDataVirtualization AllowFiltering="true" Count="@count"
                                Data=@customers TextProperty="CompanyName" ValueProperty="CustomerID" />

@code {
    IEnumerable<Customer> customers;

    void LoadDataVirtualization(LoadDataArgs args)
    {
        var query = dbContext.Customers.AsQueryable();

        if (!string.IsNullOrEmpty(args.Filter))
        {
            query = query.Where(c => c.CustomerID.ToLower().Contains(args.Filter.ToLower()) || c.ContactName.ToLower().Contains(args.Filter.ToLower()));
        }

        count = query.Count();

        customers = query.Skip(args.Skip.Value).Take(args.Top.Value).ToList();

        InvokeAsync(StateHasChanged);
    }
}
```

### Multiple selection
Bind `Value` property and use `Multiple` property to control if selection is multiple or not. 
```
<RadzenDropDown @bind-Value=@multipleValues Multiple="true" 
    Data=@customers TextProperty="CompanyName" ValueProperty="CustomerID" Change=@OnChange  />

@code {
    IEnumerable<Customer> customers;
    IEnumerable<string> multipleValues;

    protected override void OnInitialized()
    {
        customers = dbContext.Customers.ToList();
    }

    void OnChange(object value)
    {
        var str = value is IEnumerable<object> ? string.Join(", ", (IEnumerable<object>)value) : value;

        console.Log($"Value changed to {str}");
    }
}
```