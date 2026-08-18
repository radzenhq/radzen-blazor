# DataGrid: Single Column Sorting

This example demonstrates sorting in Blazor RadzenDataGrid component.

Keywords: single, sort, datagrid, table, dataview

> API reference: [RadzenDataGrid API](https://blazor.radzen.com/api/datagrid.md)

## Examples

## DataGrid Sorting

Sort a Blazor DataGrid by clicking a column header - ascending, descending, and back. Sorting is built in for any bound column.

```razor
@inherits DbContextPage

<RadzenDataGrid PageSize="5" AllowPaging="true" AllowSorting="true" Data="@employees" ColumnWidth="400px" >
    <Columns>
        <RadzenDataGridColumn Property="@nameof(Employee.FirstName)" Title="First Name" Width="150px" />
        <RadzenDataGridColumn Property="@nameof(Employee.LastName)" Title="Last Name" Width="150px"/>
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
}
```
