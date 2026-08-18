# DataGrid: Sort API

Set the initial sort order of your RadzenDataGrid via the SortOrder column property.

Keywords: api, sort, datagrid, table, dataview

> API reference: [RadzenDataGrid API](https://blazor.radzen.com/api/datagrid.md)

## Examples

## DataGrid Sort API

Set Blazor DataGrid sorting from code - apply an initial sort order with the column SortOrder property or sort programmatically.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center" class="rz-p-4 rz-border-radius-1" Style="border: var(--rz-grid-cell-border);">
    <RadzenCheckBox @bind-Value="@resetPaging" Name="resetPaging" />
    <RadzenLabel Text="Reset to first page on sort" Component="resetPaging" class="rz-me-6" />
</RadzenStack>

<RadzenDataGrid @ref="grid" AllowColumnResize="true" PageSize="5" AllowPaging="true" GotoFirstPageOnSort="@resetPaging"
                AllowSorting="true" Data="@employees" ColumnWidth="400px" Sort="@OnSort" TItem="Employee">
    <Columns>
        <RadzenDataGridColumn Property="@nameof(Employee.FirstName)" Title="First Name" SortOrder="SortOrder.Descending" Width="150px" />
        <RadzenDataGridColumn Property="@nameof(Employee.LastName)" Title="Last Name" Width="150px" />
        <RadzenDataGridColumn Property="@nameof(Employee.BirthDate)" Title="Birth Date" FormatString="{0:d}" Width="150px" />
        <RadzenDataGridColumn Property="@nameof(Employee.Country)" Title="Country" Width="150px" />
        <RadzenDataGridColumn Property="@nameof(Employee.Notes)" Title="Notes" />
    </Columns>
</RadzenDataGrid>

@code {
    RadzenDataGrid<Employee> grid;
    IEnumerable<Employee> employees;
    bool resetPaging;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        employees = dbContext.Employees;
    }

    void OnSort(DataGridColumnSortEventArgs<Employee> args)
    {
        //
    }
}
```
