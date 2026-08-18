# DataGrid: Multiple Column Sorting

This example demonstrates multiple column sorting in Blazor RadzenDataGrid component.

Keywords: multi, sort, datagrid, table, dataview

> API reference: [RadzenDataGrid API](https://blazor.radzen.com/api/datagrid.md)

## Examples

## DataGrid Multiple Column Sorting

Sort a Blazor DataGrid by multiple columns at once - users click additional headers to add secondary sort levels.

```razor
@inherits DbContextPage

<RadzenDataGrid Render="@OnRender" PageSize="5" AllowMultiColumnSorting="true" ShowMultiColumnSortingIndex="true" AllowPaging="true" AllowSorting="true" Data="@employees" ColumnWidth="400px">
    <Columns>
        <RadzenDataGridColumn Property="@nameof(Employee.FirstName)" Title="First Name" Width="150px" />
        <RadzenDataGridColumn Property="@nameof(Employee.LastName)" Title="Last Name" Width="150px" />
        <RadzenDataGridColumn Property="@nameof(Employee.BirthDate)" Title="Birth Date" FormatString="{0:d}" Width="150px" />
        <RadzenDataGridColumn Property="@nameof(Employee.Country)" Title="Country" Width="150px" />
        <RadzenDataGridColumn Property="@nameof(Employee.Notes)" Title="Notes" />
    </Columns>
</RadzenDataGrid>

@code {
    IEnumerable<Employee> employees;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        employees = dbContext.Employees;
    }

    void OnRender(DataGridRenderEventArgs<Employee> args)
    {
        if (args.FirstRender)
        {
            args.Grid.Sorts.Add(new SortDescriptor() { Property = "BirthDate", SortOrder = SortOrder.Descending });
            args.Grid.Sorts.Add(new SortDescriptor() { Property = "LastName", SortOrder = SortOrder.Ascending });
        }
    }
}
```
