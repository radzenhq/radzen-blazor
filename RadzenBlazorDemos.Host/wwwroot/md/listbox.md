# ListBox

The Blazor ListBox shows a selectable list for single or multiple selection, with filtering and virtualization for large data.

Keywords: select, picker, form, edit

> API reference: [RadzenListBox API](https://blazor.radzen.com/api/listbox.md)

## Examples

## Blazor ListBox

The Blazor ListBox shows a selectable list for single or multiple selection, with filtering and virtualization for large data.

### Get and Set the value of ListBox

As all Radzen Blazor input components the ListBox has a Value property which gets and sets the value of the component. Use `@-Value` to get the user input.

```razor
@inherits DbContextPage

<div class="rz-p-sm-12 rz-text-align-center">
    <RadzenListBox @bind-Value=@value Data=@companyNames Style="width: 100%; max-width: 400px; height:200px" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "select company" }})" />
</div>

@code {
    string value = "Around the Horn";
    IEnumerable<string> companyNames;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        companyNames = dbContext.Customers.Select(c => c.CompanyName).Distinct();
    }
}
```


### Get and Set the value of ListBox using Value and Change event

Value property can be used to set the value of the component and `Change` event to get the user input.

```razor
@inherits DbContextPage

<div class="rz-p-sm-12 rz-text-align-center">
    <RadzenListBox TValue="string" Value=@value Data=@companyNames Change="@(args => value = $"{args}")" Style="width: 100%; max-width: 400px; height: 200px"
                   InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "select company" }})" />
</div>

@code {
    string value = "Around the Horn";
    IEnumerable<string> companyNames;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        companyNames = dbContext.Customers.Select(c => c.CompanyName).Distinct();
    }
}
```


### Define Text and Value properties

Use the `TextProperty` and `ValueProperty` properties to specify which fields to display and use as values.

```razor
@inherits DbContextPage

<div class="rz-p-sm-12 rz-text-align-center">
    <RadzenListBox @bind-Value=@value Data=@customers TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)" Style="width: 100%; max-width: 400px; height: 200px"
                   InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "select company" }})" />
</div>

@code {
    string value = "AROUT";
    IEnumerable<Customer> customers;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        customers = dbContext.Customers;
    }
}
```


### ListBox with template

Use the `Template` property to customize how items are displayed in the list box.

```razor
@inherits DbContextPage

<div class="rz-p-sm-12 rz-text-align-center">
    <RadzenListBox @bind-Value=@value Data=@customers TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)" Style="width: 100%; max-width: 400px; height: 200px"
                   InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "select company" }})">
        <Template>
            Company: @((context as Customer).CompanyName)
        </Template>
    </RadzenListBox>
</div>

@code {
    string value = "AROUT";
    IEnumerable<Customer> customers;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        customers = dbContext.Customers;
    }
}
```


### ListBox multiple selection

Use `Multiple="true"` to enable selection of multiple items in the list box.

```razor
@inherits DbContextPage

<div class="rz-p-sm-12 rz-text-align-center">
    <RadzenListBox @bind-Value=@values Data=@products TextProperty="@nameof(Product.ProductName)" ValueProperty="@nameof(Product.ProductID)"
                   Multiple=true AllowClear=true Placeholder="Select products" Style="width: 100%; max-width: 400px; height: 200px"
                   InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "select products" }})" />
</div>

@code {
    IEnumerable<int> values = new int[] { 1, 2 };
    IEnumerable<Product> products;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        products = dbContext.Products;
    }
}
```


### Filtering case sensitivity and filter operator


```razor
@inherits DbContextPage

<div class="rz-p-sm-12 rz-text-align-center">
    <RadzenListBox FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive" FilterOperator="StringFilterOperator.StartsWith" AllowFiltering="true"
                   Data=@customers TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)" AllowClear="true" @bind-Value=value 
                   InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "select company" }})" Style="width: 100%; max-width: 400px; height: 200px" />
</div>

@code {
    IEnumerable<Customer> customers;
    string value;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        customers = dbContext.Customers;
    }
}
```


### Custom filtering with LoadData event

Use the `LoadData` event to implement custom filtering logic and load data on-demand.

```razor
@inherits DbContextPage

<div class="rz-p-sm-12 rz-text-align-center">
    <RadzenListBox AllowClear="true" @bind-Value=value
                    LoadData=@LoadData AllowFiltering="true"
                   Data=@customers TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)" Style="width: 100%; max-width: 400px; height: 200px"
                   InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "select company" }})" />
</div>

@code {
    IEnumerable<Customer> customers;
    string value;

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


### ListBox virtualization using IQueryable

Enable virtualization with `IQueryable` to efficiently handle large datasets by loading items on demand.

```razor
@inherits DbContextPage

<div class="rz-p-sm-12 rz-text-align-center">
    <RadzenListBox AllowClear="true" @bind-Value=value AllowVirtualization="true"
                   AllowFiltering="true" Data=@customers TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)" Style="width: 100%; max-width: 400px; height: 200px"
                   InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "select company" }})" />
</div>

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


### ListBox virtualization with LoadData event

Combine virtualization with the `LoadData` event for custom data loading with large datasets.

```razor
@inherits DbContextPage

<div class="rz-p-sm-12 rz-text-align-center">
    <RadzenListBox @bind-Value=value Data=@customers LoadData=@LoadData Count="@count" AllowVirtualization="true" AllowClear="true"
                   AllowFiltering="true" TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)" Style="width: 100%; max-width: 400px; height: 200px"
                   InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "select company" }})" />
</div>

@code {
    IEnumerable<Customer> customers;
    string value;
    int count;

    void LoadData(LoadDataArgs args)
    {
        var query = dbContext.Customers.AsQueryable();

        if (!string.IsNullOrEmpty(args.Filter))
        {
            query = query.Where(c => c.CustomerID.ToLower().Contains(args.Filter.ToLower()) || c.ContactName.ToLower().Contains(args.Filter.ToLower()));
        }

        count = query.Count();

        customers = query.Skip(args.Skip.HasValue ? args.Skip.Value : 0).Take(args.Top.HasValue ? args.Top.Value : 10).ToList();

        InvokeAsync(StateHasChanged);
    }
}
```
