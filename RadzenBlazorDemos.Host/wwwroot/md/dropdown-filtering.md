# DropDown: Filtering

Add search to the Blazor DropDown with built-in filtering. Choose the filter operator (contains, starts with), toggle case sensitivity, or filter data on demand.

Keywords: select, picker, form, edit, multiple, dropdown, filter, search

> API reference: [RadzenDropDown API](https://blazor.radzen.com/api/dropdown.md)

## Examples

## Filtering case sensitivity and filter operator

Add search to the Blazor DropDown with built-in filtering - choose the filter operator (contains, starts with), toggle case sensitivity, or filter data on demand.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Select Value" Component="DropDownFiltering" />
    <RadzenDropDown @ref=@radzenDropDown @bind-SearchText=SearchText FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive" FilterOperator="StringFilterOperator.StartsWith" AllowFiltering="true"
                    Data=@customers TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)" AllowClear="true" @bind-Value=value Style="width: 100%; max-width: 400px;" Name="DropDownFiltering" />
</RadzenStack>

<div class="rz-p-sm-3 rz-text-align-start"> 
    <RadzenLabel Text="@searchTextStatus" />
</div>

@code {
    RadzenDropDown<string> radzenDropDown;
    IEnumerable<Customer> customers;
    string value;
    string searchTextStatus;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        customers = dbContext.Customers;

        searchTextStatus = $"Search text: {searchText}";
    }

    string searchText = "al";

    public string SearchText
    {
        get
        {
            return searchText;
        }
        set
        {
            if (searchText != value)
            {
                searchText = value;
                searchTextStatus = $"Search text: {searchText}";
                Console.WriteLine($"Search text: {radzenDropDown.SearchText}");
            }
        }
    }
}
```


### Custom filtering with LoadData event


```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Select Value" Component="DropDownFilteringLoadData" />
    <RadzenDropDown AllowClear="true" @bind-Value=value
                    LoadData=@LoadData AllowFiltering="true"
                    Data=@customers TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)" Style="width: 100%; max-width: 400px;" Name="DropDownFilteringLoadData" />
</RadzenStack>

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
