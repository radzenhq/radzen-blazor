# DropDown: Virtualization

Render large Blazor DropDown lists efficiently with UI virtualization. Load items on demand from an IQueryable so only the visible options are fetched.

Keywords: select, picker, form, edit, multiple, dropdown, virtualization, paging

> API reference: [RadzenDropDown API](https://blazor.radzen.com/api/dropdown.md)

## Examples

## DropDown virtualization using IQueryable

Render large Blazor DropDown lists efficiently with UI virtualization - load items on demand from an IQueryable so only the visible options are fetched.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Select Value" Component="DropDownVirtualization" />
    <RadzenDropDown AllowClear="true" @bind-Value=value AllowVirtualization="true" Name="DropDownVirtualization"
        AllowFiltering="true" Data=@customers TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)" Style="width: 100%; max-width: 400px;" />
</RadzenStack>

@code {
    string value;
    IEnumerable<Customer> customers;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        customers = dbContext.Customers.ToList();
    }
}
```


### DropDown virtualization with LoadData event


```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Select Value" Component="DropDownVirtualizationLoadData" />
    <RadzenDropDown @bind-Value=value Data=@customers LoadData=@LoadData Count="@count" AllowVirtualization="true" AllowClear="true" Name="DropDownVirtualizationLoadData"
                    AllowFiltering="true" TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)" Style="width: 100%; max-width: 400px;" />
</RadzenStack>

@code {
    IEnumerable<Customer> customers;
    string value = "NORTS";
    int count;

    string lastfilter;
    async Task LoadData(LoadDataArgs args)
    {
        await Task.Yield();

        var query = dbContext.Customers.AsQueryable();

        if (!string.IsNullOrEmpty(args.Filter) && lastfilter != args.Filter)
        {
            args.Skip = 0;
        }

        if (!string.IsNullOrEmpty(args.Filter))
        {
            lastfilter = args.Filter;
            query = query.Where(c => c.CustomerID.ToLower().Contains(args.Filter.ToLower()) || c.ContactName.ToLower().Contains(args.Filter.ToLower()) || c.CompanyName.ToLower().Contains(args.Filter.ToLower()));
        }

        count = await Task.FromResult(query.Count());

        customers = await Task.FromResult(query.Skip(args.Skip.HasValue ? args.Skip.Value : 0).Take(args.Top.HasValue ? args.Top.Value : 10).ToList());
    
        // Load selected items if outside of visible range
        if (!string.IsNullOrEmpty(value) && !customers.Any(c => c.CustomerID == value))
        {
            var selected = await Task.FromResult(query.Where(c => c.CustomerID == value));
            customers = customers.Concat(selected);
        }
    }
}
```
