# DataGrid: Overview

A free, open-source Blazor DataGrid with sorting, filtering, paging, grouping, virtualization, inline editing, and Excel/CSV export. Bind to IQueryable, Entity Framework, OData, or any data source.

Keywords: datagrid, datatable, datagridview, grid, table, overview

> API reference: [RadzenDataGrid API](https://blazor.radzen.com/api/datagrid.md)

## Examples

## Blazor DataGrid

The Radzen Blazor DataGrid displays tabular data with sorting, filtering, paging, grouping, and editing built in. Bind it to IQueryable and Entity Framework, an OData service, a DataTable, or dynamic data, and it pages, sorts, and filters on the server.

```razor
@inherits DbContextPage

<RadzenDataGrid AllowFiltering="true" AllowColumnResize="true" AllowAlternatingRows="false" FilterMode="FilterMode.Advanced" AllowSorting="true" PageSize="5" AllowPaging="true" PagerHorizontalAlign="HorizontalAlign.Left" ShowPagingSummary="true"
    Data="@employees" ColumnWidth="300px" LogicalFilterOperator="LogicalFilterOperator.Or" SelectionMode="DataGridSelectionMode.Single" @bind-Value=@selectedEmployees>
    <Columns>
        <RadzenDataGridColumn Property="@nameof(Employee.EmployeeID)" Filterable="false" Title="ID" Frozen="true" Width="80px" TextAlign="TextAlign.Center" />
        <RadzenDataGridColumn Title="Photo" Frozen="true" Sortable="false" Filterable="false" Width="80px" TextAlign="TextAlign.Center" >
            <Template Context="data">
                <RadzenImage Path="@data.Photo" class="rz-gravatar" AlternateText="@(data.FirstName + " " + data.LastName)" />
            </Template>
        </RadzenDataGridColumn>
        <RadzenDataGridColumn Property="@nameof(Employee.FirstName)" Title="First Name" Frozen="true" Width="160px" />
        <RadzenDataGridColumn Property="@nameof(Employee.LastName)" Title="Last Name" Width="160px"/>
        <RadzenDataGridColumn Property="@nameof(Employee.Title)" Title="Job Title" Width="200px" />
        <RadzenDataGridColumn Property="@nameof(Employee.TitleOfCourtesy)" Title="Title" Width="120px" />
        <RadzenDataGridColumn Property="@nameof(Employee.BirthDate)" Title="Birth Date" FormatString="{0:d}" Width="160px" />
        <RadzenDataGridColumn Property="@nameof(Employee.HireDate)" Title="Hire Date" FormatString="{0:d}" Width="160px" />
        <RadzenDataGridColumn Property="@nameof(Employee.Address)" Title="Address" Width="200px" />
        <RadzenDataGridColumn Property="@nameof(Employee.City)" Title="City" Width="160px" />
        <RadzenDataGridColumn Property="@nameof(Employee.Region)" Title="Region" Width="160px" />
        <RadzenDataGridColumn Property="@nameof(Employee.PostalCode)" Title="Postal Code" Width="160px" />
        <RadzenDataGridColumn Property="@nameof(Employee.Country)" Title="Country" Width="160px" />
        <RadzenDataGridColumn Property="@nameof(Employee.HomePhone)" Title="Home Phone" Width="160px" />
        <RadzenDataGridColumn Property="@nameof(Employee.Extension)" Title="Extension" Width="160px" />
        <RadzenDataGridColumn Property="@nameof(Employee.Notes)" Title="Notes" Width="300px" />
    </Columns>
</RadzenDataGrid>

@code {
    IQueryable<Employee> employees;
    IList<Employee> selectedEmployees;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
 
        employees = dbContext.Employees;

        selectedEmployees = new List<Employee>(){ employees.FirstOrDefault() };
    }
}
```


### DataGrid features
