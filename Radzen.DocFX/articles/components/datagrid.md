# DataGrid component
This article demonstrates how to use the DataGrid component.

## Data-binding
To display data in DataGrid component you need to set collection of items (`IEnumerable<>`) to `Data` property and define collection of `RadzenDataGridColumn` in `Columns`. DataGrid component can perform server-side sorting, paging and filtering when bound to __IQueryable__ or using `LoadData` event. Grouping will not be performed server-side - only on current page data if paging is enabled.

### Populate data when initialized

```
<RadzenDataGrid Data="@customers" TItem="Customer">
    <Columns>
        <RadzenDataGridColumn TItem="Customer" Property="CustomerID" Title="Customer ID"  />
        <RadzenDataGridColumn TItem="Customer" Property="CompanyName" Title="Company Name" />
        <RadzenDataGridColumn TItem="Customer" Property="ContactName" Title="Contact Name" />
    </Columns>
</RadzenDataGrid>
@code {
    IEnumerable<Customer> customers;

    protected override void OnInitialized()
    {
        customers = dbContext.Customers.ToList();
    }
}
```

### Populate data on demand using LoadData event.

```
@using System.Linq.Dynamic.Core
@inject NorthwindContext dbContext

<RadzenDataGrid Data="@customers" Count="@count" TItem="Customer" LoadData="@LoadData">
    <Columns>
        <RadzenDataGridColumn TItem="Customer" Property="CustomerID" Title="Customer ID"  />
        <RadzenDataGridColumn TItem="Customer" Property="CompanyName" Title="Company Name" />
        <RadzenDataGridColumn TItem="Customer" Property="ContactName" Title="Contact Name" />
    </Columns>
</RadzenDataGrid>

@code {
    IEnumerable<Customer> customers;
    int count;

    void LoadData(LoadDataArgs args)
    {
        // This demo is using https://dynamic-linq.net
        var query = dbContext.Customers.AsQueryable();

        if (!string.IsNullOrEmpty(args.Filter))
        {
            // Filter via the Where method
            query = query.Where(args.Filter);
        }

        if (!string.IsNullOrEmpty(args.OrderBy))
        {
            // Sort via the OrderBy method
            query = query.OrderBy(args.OrderBy);
        }

        // Important!!! Make sure the Count property of RadzenDataGrid is set.
        count = query.Count();

        // Perform paging via Skip and Take.
        customers = query.Skip(args.Skip.Value).Take(args.Top.Value).ToList();
    }
}
```

## Sorting

Use `AllowSorting` and `AllowMultiColumnSorting` properties to allow and control sorting.
```
<RadzenDataGrid AllowSorting="true" AllowMultiColumnSorting="true" ...
```

By default DataGrid component will perform sorting using `Property` of the column, use `SortProperty` to specify different sort property from the property used to display data in the column. Use `Sortable` column property to enable/disable sorting for specific column. You can use dot notation to specify sub property.

```
...
<Columns>
        <RadzenDataGridColumn TItem="Order" Property="CustomerID" Title="Customer ID" Sortable="false"  />
        <RadzenDataGridColumn TItem="Order" Property="Customer.CompanyName" Title="Company Name" SortProperty="Customer.ContactName" />
        ...
```

## Paging
Use `AllowPaging` and `PageSize` properties to allow and control paging.
```
<RadzenDataGrid AllowPaging="true" PageSize="5" ...
```

## Filtering
Use `AllowFiltering`, `FilterMode`, `FilterCaseSensitivity` and `LogicalFilterOperator` properties to allow and control filtering. 
```
<RadzenDataGrid AllowFiltering="true" FilterMode="FilterMode.Advanced" LogicalFilterOperator="LogicalFilterOperator.Or" ...
```

By default DataGrid component will perform filtering using `Property` of the column, use `FilterProperty` to specify different filter property from the property used to display data in the column. Use `Filterable` column property to enable/disable filtering for specific column. You can use dot notation to specify sub property.

```
...
<Columns>
        <RadzenDataGridColumn TItem="Order" Property="CustomerID" Title="Customer ID" Filterable="false"  />
        <RadzenDataGridColumn TItem="Order" Property="Customer.CompanyName" Title="Company Name" FilterProperty="Customer.ContactName" />
        ...
```

Advanced filter mode (`FilterMode.Advanced`) allows you to to apply complex filter for each column with two filter values and filter operators while simple filter mode (`FilterMode.Simple`) allows you to apply single `FilterValue` and `FilterOperator`. `LogicalFilterOperator` column property can be used to specify how the two column filters will be applied - with `and` or `or`. 

```
...
<Columns>
    <RadzenDataGridColumn TItem="Employee" Property="FirstName" Title="First Name" 
        FilterValue=@("Nan") FilterOperator="FilterOperator.StartsWith" 
        LogicalFilterOperator="LogicalFilterOperator.Or" />
    <RadzenDataGridColumn TItem="Employee" Property="FirstName" Title="First Name" 
        FilterValue=@("Nan") FilterOperator="FilterOperator.StartsWith" 
        SecondFilterValue=@("Nan") SecondFilterOperator="FilterOperator.EndsWith"
        LogicalFilterOperator="LogicalFilterOperator.And" />
    ...
```

Use `FilterTemplate` column property to define your own custom filtering template for specific column

```
...
<Columns>
    <RadzenDataGridColumn TItem="Employee" Property="TitleOfCourtesy" Title="Title Of Courtesy" FilterValue="@currentTOC">
        <FilterTemplate>
            <RadzenDropDown @bind-Value="@currentTOC" ... />
        </FilterTemplate>
    </RadzenDataGridColumn>
        ...
```

## Grouping
Use `AllowGrouping` property to allow grouping and Groups collection to add/remove groups using `GroupDescriptor` class. By default DataGrid component will perform grouping using `Property` of the column, use `GroupProperty` to specify different group property from the property used to display data in the column. Use `Groupable` column property to enable/disable grouping for specific column. You can use dot notation to specify sub property.
```
<RadzenDataGrid AllowGrouping="true" Data="@employees" TItem="Employee" Render="@OnRender">
    <Columns>
        <RadzenDataGridColumn TItem="Employee" Property="EmployeeID" Filterable="false" Title="ID" Frozen="true" Width="70px" />
        <RadzenDataGridColumn TItem="Employee" Title="Photo" Sortable="false" Filterable="false" Groupable="false" Width="200px" >
            <Template Context="data">
                <RadzenImage Path="@data.Photo" style="width: 40px; height: 40px; border-radius: 8px;" />
            </Template>
        </RadzenDataGridColumn>
        <RadzenDataGridColumn TItem="Employee" Property="FirstName" Title="First Name" />
        <RadzenDataGridColumn TItem="Employee" Property="LastName" Title="Last Name" Width="150px"/>
    </Columns>
</RadzenDataGrid>

@code {
    IEnumerable<Employee> employees;

    protected override void OnInitialized()
    {
        employees = dbContext.Employees;
    }

    void OnRender(DataGridRenderEventArgs<Employee> args)
    {
        if(args.FirstRender)
        {
            args.Grid.Groups.Add(new GroupDescriptor(){ Property = "Title" });
            StateHasChanged();
        }
    }
}    
```

Use `GroupHeaderTemplate` to customize group headers. The context in this template is `Group` class.

```
<RadzenDataGrid AllowGrouping="true" Data="@employees" TItem="Employee" Render="@OnRender">
    <GroupHeaderTemplate>
        @context.GroupDescriptor.GetTitle(): @context.Data.Key, Group items count: @context.Data.Count, Last order date: @(context.Data.Items.Cast<Order>().OrderByDescending(o => o.OrderDate).FirstOrDefault()?.OrderDate)
    </GroupHeaderTemplate>
    ...
```

Use `GroupFooterTemplate` to customize group footers for columns. The context in this template is `Group` class.

```
<Columns>
    <RadzenDataGridColumn TItem="Order" Property="Freight" Title="Freight">
        <GroupFooterTemplate>
            Group amount: <b>@String.Format(new System.Globalization.CultureInfo("en-US"), "{0:C}", context.Data.Items.Cast<Order>().Sum(o => o.Freight))</b>
        </GroupFooterTemplate>
    </RadzenDataGridColumn>
    ...
```

## Columns

Use `Template`, `FooterTemplate` and `HeaderTemplate` to specify custom template for data, footer and header cells. 

```
...
<Columns>
    <RadzenDataGridColumn TItem="Order">
        <HeaderTemplate>
            Nr.
        </HeaderTemplate>
        <Template Context="data">
            @(orders.IndexOf(data) + 1)
        </Template>
            <FooterTemplate>
            Displayed orders: <b>@ordersGrid.View.Count()</b> of <b>@orders.Count()</b>
        </FooterTemplate>
    </RadzenDataGridColumn>
    ...
```

Use `AllowColumnResize` and `AllowColumnReorder` to allow columns resize and reorder. Use column `Resizable` and `Reorderable` properties to enable/disable resize and/or reorder for specific column. Use `ColumnWidth` to specify width for all columns or column `Width` to specify width for specific column. Use `TextAlign` column property to specify column alignment for data, header and footer cells. Set `Frozen` column property to disable horizontal scroll for specific column. Use `OnColumnResized` and `ColumnReordered` events to catch if a column is resized or reordered. 

```
<RadzenDataGrid AllowColumnResize="true" AllowColumnReorder="true" ColumnWidth="200px"
    ColumnResized=@OnColumnResized ColumnReordered="@OnColumnReordered">
    <Columns>
        <RadzenDataGridColumn TItem="Employee" Property="EmployeeID" Title="ID" Resizable="true" Reorderable="false" Frozen="true" />
        <RadzenDataGridColumn TItem="Employee" Property="FirstName" Title="FirstName" Width="50px" TextAlign="TextAlign.Center" />
        ...
```

## In-line editing

Use `EditTemplate` to specify cell template when the row is in edit mode. Use DataGrid `EditRow()`, `CancelEditRow()` and `UpdateRow()` to edit, update or cancel changes for specific data item/row. Use DataGrid `EditMode` property to specify if multiple rows or single row can be edited at once.

```
...
<RadzenDataGrid @ref="ordersGrid" EditMode="DataGridEditMode.Single"
                Data="@orders" TItem="Order" RowUpdate="@OnUpdateRow" RowCreate="@OnCreateRow">
    <Columns>
        <RadzenDataGridColumn Width="200px" TItem="Order" Property="Customer.CompanyName" Title="Customer">
            <EditTemplate Context="order">
                <RadzenDropDown @bind-Value="order.CustomerID" Data="@customers" TextProperty="CompanyName" ValueProperty="CustomerID" />
            </EditTemplate>
        </RadzenDataGridColumn>
        <RadzenDataGridColumn TItem="Order" Context="order" Filterable="false" Sortable="false" TextAlign="TextAlign.Center" Width="100px">
            <Template Context="order">
                <RadzenButton Icon="edit" Size="ButtonSize.Small" Click="@(args => EditRow(order))" @onclick:stopPropagation="true" />
            </Template>
            <EditTemplate Context="order">
                <RadzenButton Icon="save" Size="ButtonSize.Small" Click="@((args) => SaveRow(order))" />
                <RadzenButton Icon="cancel" Size="ButtonSize.Small" ButtonStyle="ButtonStyle.Secondary" Click="@((args) => CancelEdit(order))" />
            </EditTemplate>
        </RadzenDataGridColumn>
        ...
```

## Virtualization

Use `AllowVirtualization` to allow virtualization. It is supported for both __IQueryable__ or using `LoadData` event data binding. It is important to specify height for the DataGrid component.

```
<RadzenDataGrid Data="@orderDetails" TItem="OrderDetail" AllowVirtualization="true" Style="height:400px" ...
```

## Hierarchy

## Selection

## Conditional formatting
