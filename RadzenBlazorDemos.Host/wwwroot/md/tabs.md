# Tabs

The Blazor Tabs component organizes content into tabbed panels, with positioning, dynamic tabs, and lazy or client/server rendering.

Keywords: tabstrip, tabview, container

> API reference: [RadzenTabs API](https://blazor.radzen.com/api/tabs.md)

## Examples

## Blazor Tabs

The Blazor Tabs component organizes content into tabbed panels, with positioning, dynamic tabs, and lazy or client/server rendering.

### Tabs position

Use the `TabPosition` property to position tabs at the top, bottom, left, or right of the content area.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-pt-4 rz-pb-8">
    <RadzenSelectBar @bind-Value="@tabPosition" TextProperty="Text" ValueProperty="Value" 
                        Data="@(Enum.GetValues(typeof(TabPosition)).Cast<TabPosition>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" class="rz-display-none rz-display-xl-flex" />
    <RadzenDropDown @bind-Value="@tabPosition" TextProperty="Text" ValueProperty="Value"
                        Data="@(Enum.GetValues(typeof(TabPosition)).Cast<TabPosition>().Select(t => new { Text = $"{t}", Value = t }))" class="rz-display-flex rz-display-xl-none" />
</RadzenStack>

<RadzenTabs Change=@OnChange TabPosition="@tabPosition" RenderMode="TabRenderMode.Client" >
    <Tabs>
        <RadzenTabsItem Text="Orders">
            <RadzenDataList PageSize="6" WrapItems="true" AllowPaging="true" Data="@orders" TItem="Order">
                <Template Context="order">
                    <RadzenCard Style="width: 250px;" class="rz-border-radius-3">
                        <RadzenRow Gap="0.5rem">
                            <RadzenColumn Size="8" class="rz-text-truncate">
                                <RadzenBadge BadgeStyle="BadgeStyle.Light" Text=@($"{order.OrderID}") class="rz-me-1" IsPill="true" />
                                <RadzenText TextStyle="TextStyle.Caption" class="rz-mb-0">@(order.ShipName)</RadzenText>
                            </RadzenColumn>
                            <RadzenColumn Size="4" class="rz-text-align-end">
                                <RadzenBadge BadgeStyle="BadgeStyle.Secondary" Shade="Shade.Lighter" Text=@($"{String.Format(new System.Globalization.CultureInfo("en-US"), "{0:C}", order.Freight)}") IsPill="true" />
                            </RadzenColumn>
                        </RadzenRow>
                        <hr style="border: none; background-color: rgba(0,0,0,.08); height: 1px; margin: 1rem 0;" />
                        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem">
                            <RadzenImage Path="@order.Employee?.Photo" class="rz-border-radius-10" Style="width: 80px; height: 80px;" AlternateText="@(order.Employee?.FirstName + " " + order.Employee?.LastName)" />
                            <RadzenStack Gap="0">
                                <RadzenText TextStyle="TextStyle.Subtitle1" TagName="TagName.P" class="rz-mb-0"><strong>@(order.Employee?.FirstName + " " + order.Employee?.LastName)</strong></RadzenText>
                                <RadzenText TextStyle="TextStyle.Body2" class="rz-mb-0">@order.ShipAddress</RadzenText>
                                <RadzenText TextStyle="TextStyle.Caption" class="rz-mb-0">@(order.ShipCity), @(order.ShipCountry)</RadzenText>
                            </RadzenStack>
                        </RadzenStack>
                    </RadzenCard>
                </Template>
            </RadzenDataList>
        </RadzenTabsItem>
        <RadzenTabsItem Text="Employee">
            <RadzenDataGrid ColumnWidth="150px" AllowFiltering="true" AllowPaging="true" PageSize="5" AllowSorting="true" Data="@employees">
                <Columns>
                    <RadzenDataGridColumn Property="@nameof(Employee.Photo)" Title="Photo" Sortable="false" Filterable="false" Width="80px">
                        <Template Context="data">
                            <RadzenImage Path="@data?.Photo" Style="width: 40px; height: 40px; border-radius: 8px;" AlternateText="@(data.FirstName + " " + data.LastName)" />
                        </Template>
                    </RadzenDataGridColumn>
                    <RadzenDataGridColumn Property="@nameof(Employee.LastName)" Title="Last Name" />
                    <RadzenDataGridColumn Property="@nameof(Employee.FirstName)" Title="First Name" />
                    <RadzenDataGridColumn Property="@nameof(Employee.EmployeeID)" Title="Employee ID" />
                    <RadzenDataGridColumn Property="@nameof(Employee.Title)" Title="Title" />
                    <RadzenDataGridColumn Property="@nameof(Employee.BirthDate)" Title="Birth Date">
                        <Template Context="data">
                            @String.Format("{0:d}", data.BirthDate)
                        </Template>
                    </RadzenDataGridColumn>
                    <RadzenDataGridColumn Property="@nameof(Employee.HireDate)" Title="Hire Date">
                        <Template Context="data">
                            @String.Format("{0:d}", data.HireDate)
                        </Template>
                    </RadzenDataGridColumn>
                    <RadzenDataGridColumn Property="@nameof(Employee.Address)" Title="Address" />
                    <RadzenDataGridColumn Property="@nameof(Employee.City)" Title="City" />
                    <RadzenDataGridColumn Property="@nameof(Employee.Region)" Title="Region" />
                    <RadzenDataGridColumn Property="@nameof(Employee.PostalCode)" Title="Postal Code" />
                    <RadzenDataGridColumn Property="@nameof(Employee.Country)" Title="Country" />
                    <RadzenDataGridColumn Property="@nameof(Employee.HomePhone)" Title="Home Phone" />
                    <RadzenDataGridColumn Property="@nameof(Employee.Extension)" Title="Extension" />
                    <RadzenDataGridColumn Property="@nameof(Employee.Notes)" Title="Notes" />
                </Columns>
            </RadzenDataGrid>
        </RadzenTabsItem>
        <RadzenTabsItem Text="Customers">
            <RadzenDataGrid ColumnWidth="150px" AllowFiltering="true" AllowPaging="true" PageSize="8" AllowSorting="true" Data="@customers" TItem="Customer">
                <Columns>
                    <RadzenDataGridColumn Property="CustomerID" Title="Customer ID" />
                    <RadzenDataGridColumn Property="CompanyName" Title="Company Name" />
                    <RadzenDataGridColumn Property="ContactName" Title="Contact Name" />
                    <RadzenDataGridColumn Property="ContactTitle" Title="Contact Title" />
                    <RadzenDataGridColumn Property="@nameof(Employee.Address)" Title="Address" />
                    <RadzenDataGridColumn Property="@nameof(Employee.City)" Title="City" />
                    <RadzenDataGridColumn Property="@nameof(Employee.Region)" Title="Region" />
                    <RadzenDataGridColumn Property="@nameof(Employee.PostalCode)" Title="Postal Code" />
                    <RadzenDataGridColumn Property="@nameof(Employee.Country)" Title="Country" />
                    <RadzenDataGridColumn Property="Phone" Title="Phone" />
                    <RadzenDataGridColumn Property="Fax" Title="Fax" />
                </Columns>
            </RadzenDataGrid>
        </RadzenTabsItem>
    </Tabs>
</RadzenTabs>

<EventConsole @ref=@console />

@code {
    EventConsole console;
    IEnumerable<Order> orders;
    IEnumerable<Employee> employees;
    IEnumerable<Customer> customers;

    TabPosition tabPosition = TabPosition.Top;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        employees = dbContext.Employees.ToList();

        customers = dbContext.Customers.ToList();

        orders = dbContext.Orders.Include("Customer").Include("Employee").ToList();
    }

    void OnChange(int index)
    {
        console.Log($"Tab with index {index} was selected.");
    }
}
```


### Server render mode

Only selected tab content will be rendered.

```razor
<RadzenTabs @bind-SelectedIndex=@selectedIndex>
    <Tabs>
        <RadzenTabsItem Text="Customers">
            Customers
        </RadzenTabsItem>
        <RadzenTabsItem Text="Orders">
            Orders
        </RadzenTabsItem>
        <RadzenTabsItem Text="Order Details">
            Order Details
        </RadzenTabsItem>
    </Tabs>
</RadzenTabs>

@code {
    int selectedIndex = 0;
}
```


### Client render mode

All tabs will be rendered initially and tab change will be performed completely using JavaScript

```razor
<RadzenTabs RenderMode="TabRenderMode.Client" @bind-SelectedIndex=@selectedIndex>
    <Tabs>
        <RadzenTabsItem Text="Customers">
            Customers
        </RadzenTabsItem>
        <RadzenTabsItem Text="Orders">
            Orders
        </RadzenTabsItem>
        <RadzenTabsItem Text="Order Details">
            Order Details
        </RadzenTabsItem>
    </Tabs>
</RadzenTabs>

@code {
    int selectedIndex = 0;
}
```


### TabItems modify

Demonstrating modifications to the TabItem collection

```razor
<RadzenTabs @ref="tabs" RenderMode="TabRenderMode.Client" @bind-SelectedIndex=@selectedIndex>
    <Tabs>
        @foreach (var item in items)
        {
            <RadzenTabsItem Text="@(item)">
                @item
            </RadzenTabsItem>
        }
    </Tabs>
</RadzenTabs>

<RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-pt-4 rz-pb-8">
    <RadzenButton Click="AddItem">Add Item</RadzenButton>
    <RadzenButton Click="RemoveItem">Remove Item</RadzenButton>
</RadzenStack>

@code {
    RadzenTabs tabs;
    int selectedIndex = 0;

    List<string> items = new List<string> { "Customers", "Orders", "Order Details" };
    int i = 0;

    void AddItem()
    {
        items.Add($"TabItem{++i}");
        tabs.Reload();
    }

    void RemoveItem()
    {
        items.RemoveAt(selectedIndex);
        if (selectedIndex >= items.Count) selectedIndex = items.Count - 1;
        tabs.Reload();
    }
}
```


### Tab items wrap

Demonstrating wrap of the Tab items

```razor
<style>
    ul[role=tablist] {
        flex-wrap: wrap;
    }
</style>
<RadzenTabs>
    <Tabs>
        @foreach (var i in Enumerable.Range(0,20))
        {
            <RadzenTabsItem Text="@("Tab" + i)">
                @("Tab" + i)
            </RadzenTabsItem>
        }
    </Tabs>
</RadzenTabs>
```


### Prevent Tab change

Demonstrating how to prevent Tab change

```razor
<RadzenTabs>
    <Tabs>
        <RadzenTabsItem Text="Tab1">
            Tab1
            <RadzenButton Text="Enable/disable second tab" Click="@(args => disabled = !disabled)" />
        </RadzenTabsItem>
        <RadzenTabsItem Text="Tab2" Disabled=@disabled>
            Tab2
        </RadzenTabsItem>
    </Tabs>
</RadzenTabs>

@code {
    bool disabled = true;
}
```


### Reorder tabs

Set `AllowReorder` to `true` to enable drag and drop reordering of tabs.

```razor
<RadzenTabs AllowReorder="true" @bind-SelectedIndex=@selectedIndex Reorder=@OnReorder>
    <Tabs>
        <RadzenTabsItem Text="Customers" Icon="account_circle">
            Customers
        </RadzenTabsItem>
        <RadzenTabsItem Text="Orders" Icon="shopping_cart">
            Orders
        </RadzenTabsItem>
        <RadzenTabsItem Text="Order Details" Icon="receipt">
            Order Details
        </RadzenTabsItem>
        <RadzenTabsItem Text="Products" Icon="inventory_2">
            Products
        </RadzenTabsItem>
    </Tabs>
</RadzenTabs>

<EventConsole @ref=@console />

@code {
    EventConsole console;
    int selectedIndex = 0;

    void OnReorder(TabsReorderEventArgs args)
    {
        console.Log($"Tab moved from index {args.OldIndex} to index {args.NewIndex}.");
    }
}
```
