# DataGrid: IQueryable

Use RadzenDataGrid to display tabular data with ease. Perform paging, sorting and filtering through Entity Framework without extra code.

Keywords: datatable, datagridview, dataview, grid, table

> API reference: [RadzenDataGrid API](https://blazor.radzen.com/api/datagrid.md)

## Examples

## DataGrid IQueryable

Display tabular data with ease. Perform paging, sorting and filtering through Entity Framework without extra code.

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
