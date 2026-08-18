# DropDownDataGrid

Show tabular data inside a dropdown with the Blazor DropDownDataGrid - multiple columns, filtering, paging, and single or multiple selection.

Keywords: select, picker, form, edit, dropdown, grid, multiselect

> API reference: [RadzenDropDownDataGrid API](https://blazor.radzen.com/api/dropdowndatagrid.md)

## Examples

## Blazor DropDownDataGrid

Show tabular data inside a dropdown with the Blazor DropDownDataGrid - multiple columns, filtering, paging, and single or multiple selection.

### Get and Set the value of DropDownDataGrid

As all Radzen Blazor input components the DropDownDataGrid has a Value property which gets and sets the value of the component. Use `@-Value` to get the user input.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Value" Component="DropDownDataGridBindValue" />
    <RadzenDropDownDataGrid @bind-Value=@value Data=@companyNames Name="DropDownDataGridBindValue" />
</RadzenStack>

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


### Get and Set the value of DropDownDataGrid using Value and Change event

Value property can be used to set the value of the component and `Change` event to get the user input.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Value" Component="DropDownDataGridChangeEvent" />
    <RadzenDropDownDataGrid TValue="string" Value=@value Data=@companyNames Change="@(args => value = $"{args}")" Name="DropDownDataGridChangeEvent" />
</RadzenStack>

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

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Value" Component="DropDownDataGridTextValueProperties" />
    <RadzenDropDownDataGrid @bind-Value=@value Data=@customers TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)" Name="DropDownDataGridTextValueProperties" />
</RadzenStack>

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


### DropDownDataGrid with custom header, footer, value and item templates

Use the `HeaderTemplate`, `FooterTemplate`, `ValueTemplate`, and `Template` properties to customize the appearance.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Value" Component="DropDownDataGridTemplate" />
    <RadzenDropDownDataGrid @bind-Value=@value Data=@customers TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)" Name="DropDownDataGridTemplate">
        <HeaderTemplate>
            Custom header
        </HeaderTemplate>
        <Template>
            Company: @((context as Customer).CompanyName)
        </Template>
        <ValueTemplate>
            Company: @((context as Customer).CompanyName)
        </ValueTemplate>
        <FooterTemplate>
            <RadzenStack AlignItems="AlignItems.Center">Custom footer</RadzenStack>
        </FooterTemplate>
    </RadzenDropDownDataGrid>
</RadzenStack>

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


### Define multiple columns

Add multiple `RadzenDropDownDataGridColumn` components to display additional data columns in the dropdown grid.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Value" Component="DropDownDataGridColumns" />
    <RadzenDropDownDataGrid @bind-Value=@value Data=@customers TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)"
                            AllowColumnResize="true" AllowFilteringByAllStringColumns="true" Name="DropDownDataGridColumns">
        <Columns>
            <RadzenDropDownDataGridColumn Property="@nameof(Customer.CustomerID)" Title="CustomerID" Width="100px" />
            <RadzenDropDownDataGridColumn Property="@nameof(Customer.CompanyName)" Title="CompanyName" Width="200px" />
            <RadzenDropDownDataGridColumn Property="@nameof(Customer.City)" Title="City" Width="100px" />
            <RadzenDropDownDataGridColumn Property="@nameof(Customer.Country)" Title="Country" Width="100px" />
        </Columns>
    </RadzenDropDownDataGrid>
</RadzenStack>

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


### Filtering case sensitivity and filter operator


```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Value" Component="DropDownDataGridFiltering" />
    <RadzenDropDownDataGrid FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive" FilterOperator="StringFilterOperator.StartsWith" AllowFiltering="true"
                    Data=@customers TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)" AllowClear="true" @bind-Value=value Name="DropDownDataGridFiltering" />
</RadzenStack>

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

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Value" Component="DropDownDataGridFilteringLoadData" />
    <RadzenDropDownDataGrid AllowClear="true" @bind-Value=value
                    LoadData=@LoadData AllowFiltering="true"
                    Data=@customers Count=@count TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)" Name="DropDownDataGridFilteringLoadData" />
</RadzenStack>

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

        if (!string.IsNullOrEmpty(args.OrderBy))
        {
            query = query.OrderBy(args.OrderBy);
        }

        if (args.Skip != null)
        {
            query = query.Skip(args.Skip.Value);
        }

        if (args.Top != null)
        {
            query = query.Take(args.Top.Value);
        }

        customers = query.ToList();

        InvokeAsync(StateHasChanged);
    }
}
```


### Multiple selection

Use `Multiple="true"` to enable selection of multiple items from the dropdown grid.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Values" Component="DropDownDataGridMultiple" />
    <RadzenDropDownDataGrid @ref="grid" Chips="true" AllowFiltering="true" FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive" AllowClear="true" @bind-Value=@values
                            Multiple="true" Placeholder="Select..." Data=@customers TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)" Name="DropDownDataGridMultiple">
        <Columns>
            <RadzenDropDownDataGridColumn Width="60px" Sortable="false">
                <HeaderTemplate>
                    <RadzenCheckBox InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "select all" }})" Disabled="@(!grid.AllowSelectAll)" TriState="false" TValue="bool" Value="@(customers.Any(c => values != null && values.Contains(c.CustomerID)))"
                                    Change="@(args => values = args ? grid.View.Cast<Customer>().Select(c => c.CustomerID) : values = Enumerable.Empty<string>())" />
                </HeaderTemplate>
                <Template Context="data">
                    <RadzenCheckBox InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "select item" }})" TriState="false" Value="@(values != null && values.Contains(((Customer) data).CustomerID))"
                                    TValue="bool" Change=@(args => grid.SelectItem(data)) @onclick:stopPropagation/>
                </Template>
            </RadzenDropDownDataGridColumn>
            <RadzenDropDownDataGridColumn Property="@nameof(Customer.CustomerID)" Title="CustomerID" Width="80px" />
            <RadzenDropDownDataGridColumn Property="@nameof(Customer.CompanyName)" Title="CompanyName" Width="200px" />
            <RadzenDropDownDataGridColumn Property="@nameof(Customer.City)" Title="City" Width="100px" />
            <RadzenDropDownDataGridColumn Property="@nameof(Customer.Country)" Title="Country" Width="100px" />
        </Columns>
    </RadzenDropDownDataGrid>
</RadzenStack>

@code {
    RadzenDropDownDataGrid<IEnumerable<string>> grid;
    IEnumerable<Customer> customers;

    IEnumerable<string> values = new string[] { "ALFKI", "AROUT" };

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        customers = dbContext.Customers;
    }
}
```


### DropDownDataGrid virtualization with IQueryable event

Enable virtualization with `IQueryable` to efficiently handle large datasets by loading items on demand.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Value" Component="DropDownDataGridVirtualization" />
    <RadzenDropDownDataGrid AllowClear="true" @bind-Value=value AllowVirtualization="true"
            AllowFiltering="true" Data=@customers TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)" Name="DropDownDataGridVirtualization" />
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


### DropDownDataGrid virtualization with LoadData event

Combine virtualization with the `LoadData` event for custom data loading with large datasets.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Value" Component="DropDownDataGridVirtualizationLoadData" />
    <RadzenDropDownDataGrid @bind-Value=value Data=@customers LoadData=@LoadData Count="@count" AllowVirtualization="true" AllowClear="true"
                    AllowFiltering="true" TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)" Name="DropDownDataGridVirtualizationLoadData" />
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

        if (!string.IsNullOrEmpty(args.OrderBy))
        {
            query = query.OrderBy(args.OrderBy);
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


### Control DropDown's DataGrid Density with Density parameter

Use the `Density` property to control the spacing and compactness of rows in the dropdown grid.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Value" Component="DropDownDataGridDensity" />
    <RadzenDropDownDataGrid @bind-Value=@value Data=@companyNames Density="Density.Compact" Name="DropDownDataGridDensity" />
</RadzenStack>

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


### DropDownDataGrid binding to dynamic data

Bind the DropDownDataGrid to dynamic data sources where columns and data are determined at runtime.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-12">
    <RadzenLabel Text="Select Value" Component="DropDownDataGridBindValue" />
    <RadzenDropDownDataGrid GridLines="Radzen.DataGridGridLines.Both" @ref=grid Data="@data" ColumnWidth="200px" TValue="IDictionary<string, object>" 
                            AllowFiltering="true" AllowSorting="true" Value="@selectedItem" Change="@OnChange"
                            TextProperty="@(PropertyAccess.GetDynamicPropertyExpression("LastName", typeof(string)))">
        <ValueTemplate>
            @string.Join(", ", columns.Where(c => c.Value == typeof(string)).Take(grid.MaxSelectedLabels).Select(c => context[c.Key]))
        </ValueTemplate>
        <Columns>
            @foreach (var column in columns)
            {
                <RadzenDropDownDataGridColumn @key=@column.Key Title="@column.Key" Type="column.Value"
                                              Property="@PropertyAccess.GetDynamicPropertyExpression(column.Key, column.Value)">
                    <Template>
                        @context[@column.Key]
                    </Template>
                </RadzenDropDownDataGridColumn>
            }
        </Columns>
    </RadzenDropDownDataGrid>
</RadzenStack>

@code {
    RadzenDropDownDataGrid<IDictionary<string, object>> grid;
    IDictionary<string, object> selectedItem;

    public IEnumerable<IDictionary<string, object>> data { get; set; }

    public IDictionary<string, Type> columns { get; set; }

    public enum EnumTest
    {
        EnumValue1,
        EnumValue2
    }

    void OnChange(object value)
    {
        selectedItem = (IDictionary<string, object>)value;
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        columns = new Dictionary<string, Type>()
        {
            { "EmployeeID", typeof(int) },
            { "MyColumn", typeof(EnumTest) },
            { "FirstName", typeof(string) },
            { "LastName", typeof(string) },
            { "HireDate", typeof(DateTime) },
        };

        foreach (var i in Enumerable.Range(0, 50))
        {
            columns.Add($"Column{i}", typeof(string));
        }

        data = Enumerable.Range(0, 100).Select(i =>
        {
            var row = new Dictionary<string, object>();

            foreach (var column in columns)
            {
                row.Add(
                    column.Key,
                    column.Value == typeof(EnumTest)
                        ? (i % 2 == 0 ? EnumTest.EnumValue1 : EnumTest.EnumValue2)
                        : column.Value == typeof(int)
                            ? i
                            : column.Value == typeof(DateTime)
                                ? DateTime.Now.AddMonths(i)
                                : $"{column.Key}{i}"
                );
            }

            return row;
        });
    }
}
```


### DropDownDataGrid Sizes

Use the `InputSize` property to set the DropDownDataGrid size. Available sizes are ExtraSmall, Small, Medium (default), and Large.

```razor
@inherits DbContextPage

<RadzenStack Gap="1rem" class="rz-p-sm-12">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Large" Style="width: 80px;" />
        <RadzenDropDownDataGrid @bind-Value=@value Data=@companyNames InputSize="InputSize.Large" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Medium" Style="width: 80px;" />
        <RadzenDropDownDataGrid @bind-Value=@value Data=@companyNames InputSize="InputSize.Medium" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Small" Style="width: 80px;" />
        <RadzenDropDownDataGrid @bind-Value=@value Data=@companyNames InputSize="InputSize.Small" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Extra Small" Style="width: 80px;" />
        <RadzenDropDownDataGrid @bind-Value=@value Data=@companyNames InputSize="InputSize.ExtraSmall" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
</RadzenStack>

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
