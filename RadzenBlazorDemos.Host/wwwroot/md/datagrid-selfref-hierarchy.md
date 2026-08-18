# DataGrid: Self-reference hierarchy

This example demonstrates how to develop and show a self-referencing hierarchy.

Keywords: master, detail, datagrid, table, dataview, hierarchy, self-reference

> API reference: [RadzenDataGrid API](https://blazor.radzen.com/api/datagrid.md)

## Examples

## DataGrid self-Reference hierarchy

Display a self-referencing hierarchy in a Blazor DataGrid - render parent/child rows from a single self-related table, like an org chart or category tree.

```razor
@inherits DbContextPage

<RadzenDataGrid @ref="grid" AllowFiltering="true" AllowSorting="true" AllowColumnResize="true"
                Data="@employees" RowRender="@RowRender" LoadChildData="@LoadChildData" TItem="Employee"
                RowCollapse="@(args => grid.ColumnsCollection.ToList().ForEach(c => c.ClearFilters()))">
    <Columns>
        <RadzenDataGridColumn Title="Employee" Frozen="true" Sortable="false" Filterable="false" Width="300px">
            <Template Context="data">
                <RadzenImage Path="@data.Photo" class="rz-gravatar rz-me-1" AlternateText="@(data.FirstName + " " + data.LastName)" />
                <strong>@data.TitleOfCourtesy @data.FirstName @data.LastName</strong>
            </Template>
        </RadzenDataGridColumn>
        <RadzenDataGridColumn Property="@nameof(Employee.Title)" Title="Job Title" Width="240px" />
        <RadzenDataGridColumn Property="@nameof(Employee.HireDate)" Title="Hire Date" FormatString="{0:d}" Width="160px" />
        <RadzenDataGridColumn Property="@nameof(Employee.City)" Title="City" Width="200px" />
        <RadzenDataGridColumn Property="@nameof(Employee.HomePhone)" Title="Home Phone" Width="200px" />
        <RadzenDataGridColumn Property="@nameof(Employee.Extension)" Title="Extension" />
    </Columns>
</RadzenDataGrid>

@code {
    IEnumerable<Employee> employees;
    RadzenDataGrid<Employee> grid;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        employees = dbContext.Employees.Where(e => e.ReportsTo == null);
    }

    void RowRender(RowRenderEventArgs<Employee> args)
    {
        args.Expandable = dbContext.Employees.Where(e => e.ReportsTo == args.Data.EmployeeID).Any();
    }

    void LoadChildData(DataGridLoadChildDataEventArgs<Employee> args)
    {
        args.Data = dbContext.Employees.Where(e => e.ReportsTo == args.Item.EmployeeID);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        base.OnAfterRender(firstRender);

        if (firstRender)
        {
            await grid.ExpandRow(employees.FirstOrDefault());
        }
    }
}
```
