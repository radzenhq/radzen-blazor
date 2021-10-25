# Accordion component
This article demonstrates how to use the AutoComplete component.

## Populate data when initialized
```
<RadzenAutoComplete Data="@customers" TextProperty="CompanyName" Change="@Changed" />
@code {
    IEnumerable<Customer> customers;

    protected override void OnInitialized()
    {
        customers = dbContext.Customers.ToList();
    }

    void OnChange(object value)
    {
        Console.WriteLine($"Value changed to {value}");
    }
}
```

## Populate data on demand using LoadData event.
```
<RadzenAutoComplete Data="@customers" TextProperty="CompanyName" Change="@Changed" LoadData=@OnLoadData />
@code {
    IEnumerable<Customer> customers;

    void OnChange(object value)
    {
        Console.WriteLine($"Value changed to {value}");
    }

    void OnLoadData(LoadDataArgs args)
    {
        Console.WriteLine($"LoadData with filter: {args.Filter}");

        customers = dbContext.Customers.Where(c => c.CustomerID.Contains(args.Filter) || c.ContactName.Contains(args.Filter)).ToList();

        InvokeAsync(StateHasChanged);
    }
}
```
