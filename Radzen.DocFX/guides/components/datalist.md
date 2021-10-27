# DataList component
This article demonstrates how to use the DataList component.

## Data-binding
To display data in DataList component you need to set collection of items (`IEnumerable<>`) to `Data` property. DataGrid component can perform server-side paging when bound to __IQueryable__ or using `LoadData` event.

### Populate data when initialized

```
@inject NorthwindContext dbContext

<RadzenDataList WrapItems="true" AllowPaging="true" Data="@orders" TItem="Order">
    <Template Context="order">
        <div>Company:</div>
        <b>@order.Customer?.CompanyName</b>
    </Template>
</RadzenDataList>

@code {
    IEnumerable<Order> orders;

    protected override void OnInitialized()
    {
        orders = dbContext.Orders.Include("Customer").Include("Employee").ToList();
    }
}
```

### Populate data on demand using LoadData event.

```
@using System.Linq.Dynamic.Core
@inject NorthwindContext dbContext

<RadzenDataList WrapItems="true" AllowPaging="true" Data="@orders" Count="@count" TItem="Order" LoadData="@LoadData">
    <Template Context="order">
        <div>Company:</div>
        <b>@order.Customer?.CompanyName</b>
    </Template>
</RadzenDataList>

@code {
    IEnumerable<Customer> customers;
    int count;

    void LoadData(LoadDataArgs args)
    {
        // This demo is using https://dynamic-linq.net
        var query = dbContext.Customers.AsQueryable();

        count = query.Count();

        // Perform paging via Skip and Take.
        customers = query.Skip(args.Skip.Value).Take(args.Top.Value).ToList();
    }
}
```
