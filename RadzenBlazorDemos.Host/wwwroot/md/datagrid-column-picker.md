# DataGrid: Column Picker

Enable column picker in RadzenDataGrid by setting the AllowColumnPicking property to true.

Keywords: datagrid, column, picker, chooser

> API reference: [RadzenDataGrid API](https://blazor.radzen.com/api/datagrid.md)

## Examples

## DataGrid Column Picker

Add a column picker to a Blazor DataGrid so users can show and hide columns at runtime to focus on the data they care about.
Documentation: grid, column.

```razor
@inherits DbContextPage

<RadzenDataGrid Data="@employees"
                TItem="Employee"
                AllowFiltering=true
                ColumnWidth="300px"
                AllowColumnPicking="true"
                PickedColumnsChanged="@PickedColumnsChanged"
                ColumnsPickerAllowFiltering="true" QueryOnlyVisibleColumns="true">
    <Columns>
        <RadzenDataGridColumn Property=@nameof(Employee.EmployeeID)
                              Title="ID"
                              Width="80px"
                              TextAlign="TextAlign.Center"
                              Frozen="true" />
        <RadzenDataGridColumn Property="@nameof(Employee.Photo)" Title="Photo" Sortable="false" Filterable="false" Width="200px" Pickable="false">
            <Template Context="data">
                <RadzenImage Path="@data.Photo" class="rz-gravatar" AlternateText="@(data.FirstName + " " + data.LastName)" />
            </Template>
        </RadzenDataGridColumn>
        <RadzenDataGridColumn Property=@nameof(Employee.FirstName) Title="First Name" />
        <RadzenDataGridColumn Property=@nameof(Employee.LastName) Title="Last Name" Width="150px" />
        <RadzenDataGridColumn Property=@nameof(Employee.Title) Title="Title" />
        <RadzenDataGridColumn Property=@nameof(Employee.TitleOfCourtesy) Title="Title Of Courtesy" />
        <RadzenDataGridColumn Property=@nameof(Employee.BirthDate) Title="Birth Date" FormatString="{0:d}" />
        <RadzenDataGridColumn Property=@nameof(Employee.HireDate) Title="Hire Date" FormatString="{0:d}" />
        <RadzenDataGridColumn Property=@nameof(Employee.Address) Title="Address" />
        <RadzenDataGridColumn Property=@nameof(Employee.City) Title="City" />
        <RadzenDataGridColumn Property=@nameof(Employee.Region) Title="Region" />
        <RadzenDataGridColumn Property=@nameof(Employee.PostalCode) Title="Postal Code" />
        <RadzenDataGridColumn Property=@nameof(Employee.Country) Title="Country" />
        <RadzenDataGridColumn Property=@nameof(Employee.HomePhone) Title="Home Phone" />
        <RadzenDataGridColumn Property=@nameof(Employee.Extension) Title="Extension" ColumnPickerTitle="Phone Number Extension" />
        <RadzenDataGridColumn Property=@nameof(Employee.Notes) Title="Notes" />
    </Columns>
</RadzenDataGrid>
<EventConsole @ref=@console />

@code {
    IEnumerable<Employee> employees;
    EventConsole console;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        employees = dbContext.Employees;
    }

    /// <summary>
    /// Only used to print the changes to the console.
    /// </summary>
    void PickedColumnsChanged(DataGridPickedColumnsChangedEventArgs<Employee> args)
    {
        console.Log($"Picked columns: {string.Join(", ", args.Columns.Select(c => c.Title))}");
    }
}
```


### Picking columns with Context Menu

You could also create your own custom column picker using the context menu. In this case you will have to track column visibility yourself.

```razor
@inherits DbContextPage

<RadzenStack ContextMenu=@ShowContextMenuColumnPicker>
    <RadzenDataGrid
        @ref=grid
        Data="@employees"
        TItem="Employee"
        ColumnWidth="300px"
        AllowColumnPicking="false"
    >
        <Columns>
            <RadzenDataGridColumn
                @attributes=@GetAttributes(nameof(Employee.EmployeeID))
                Title="ID"
                Width="80px"
                TextAlign="TextAlign.Center"
                Frozen="true"
            />
            <RadzenDataGridColumn Title="Photo" Sortable="false" Width="200px" Pickable="false">
                <Template Context="data">
                    <RadzenImage Path="@data.Photo" class="rz-gravatar" AlternateText="@(data.FirstName + " " + data.LastName)" />
                </Template>
            </RadzenDataGridColumn>
            <RadzenDataGridColumn @attributes=@GetAttributes(nameof(Employee.FirstName)) Title="First Name" />
            <RadzenDataGridColumn @attributes=@GetAttributes(nameof(Employee.LastName)) Title="Last Name" Width="150px"/>
            <RadzenDataGridColumn @attributes=@GetAttributes(nameof(Employee.Title)) Title="Title" />
            <RadzenDataGridColumn @attributes=@GetAttributes(nameof(Employee.TitleOfCourtesy)) Title="Title Of Courtesy" />
            <RadzenDataGridColumn @attributes=@GetAttributes(nameof(Employee.BirthDate)) Title="Birth Date" FormatString="{0:d}" />
            <RadzenDataGridColumn @attributes=@GetAttributes(nameof(Employee.HireDate)) Title="Hire Date" FormatString="{0:d}" />
            <RadzenDataGridColumn @attributes=@GetAttributes(nameof(Employee.Address)) Title="Address" />
            <RadzenDataGridColumn @attributes=@GetAttributes(nameof(Employee.City)) Title="City" />
            <RadzenDataGridColumn @attributes=@GetAttributes(nameof(Employee.Region)) Title="Region" />
            <RadzenDataGridColumn @attributes=@GetAttributes(nameof(Employee.PostalCode)) Title="Postal Code" />
            <RadzenDataGridColumn @attributes=@GetAttributes(nameof(Employee.Country)) Title="Country" />
            <RadzenDataGridColumn @attributes=@GetAttributes(nameof(Employee.HomePhone)) Title="Home Phone" />
            <RadzenDataGridColumn @attributes=@GetAttributes(nameof(Employee.Extension)) Title="Extension" />
            <RadzenDataGridColumn @attributes=@GetAttributes(nameof(Employee.Notes)) Title="Notes" />
        </Columns>
    </RadzenDataGrid>
</RadzenStack>
<EventConsole @ref=@console />

@code {
    IEnumerable<Employee> employees;
    EventConsole console;
    RadzenDataGrid<Employee> grid;

    /// <summary>
    /// Stores the set of selected column property names.
    /// When null - all columns are selected.
    /// </summary>
    private HashSet<string> selectedColumns;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        employees = dbContext.Employees;
    }

    /// <summary>
    /// Returns a set of key-value pairs to splat as column attributes.
    /// </summary>
    IEnumerable<KeyValuePair<string, object>> GetAttributes(string columnName)
    {
        var isSelected = selectedColumns?.Contains(columnName) ?? true;
        return new KeyValuePair<string, object>[] {
            new ("Property", columnName),
            new ("Visible", isSelected),
        };
    }

    /// <summary>
    /// Displays the column picker in a context menu.
    /// </summary>
    void ShowContextMenuColumnPicker(MouseEventArgs args)
    {
        ContextMenuService.Open(args, ds =>
            @<RadzenListBox
                TValue="object"
                Style="height:200px;background-color:#fff"
                SelectAllText="All"
                AllowSelectAll="true"
                AllowFiltering="true"
                FilterCaseSensitivity=FilterCaseSensitivity.CaseInsensitive
                Multiple="true"
                Placeholder="All"
                Data="@grid.ColumnsCollection.Where(c => c.Pickable)"
                TextProperty="Title"
                Value="@(grid.ColumnsCollection.Where(c => c.Pickable && c.GetVisible()))"
                Change=@RadzenListBoxSelectionChange
            />
        );
    }

    /// <summary>
    /// Reacts to the changes of selected columns in the context menu.
    /// </summary>
    void RadzenListBoxSelectionChange(object args)
    {
        var selectedColumnNames = ((IEnumerable<object>)args)
            .Cast<RadzenDataGridColumn<Employee>>()
            .Select(c => c.Title);

        console.Log($"Picked columns: {string.Join(", ", selectedColumnNames)}");

        this.selectedColumns = ((IEnumerable<object>)args)
            .Cast<RadzenDataGridColumn<Employee>>()
            .Select(c => c.Property)
            .ToHashSet();
    }
}
```


### Picking columns with Sorting

You can also add sorting to the column picker so that users can view the columns in descending or ascending order.

```razor
@inherits DbContextPage

<RadzenDataGrid Data="@employees"
                TItem="Employee"
                AllowFiltering=true
                ColumnWidth="300px"
                AllowColumnPicking="true"
                PickedColumnsChanged="@PickedColumnsChanged"
                ColumnsPickerAllowFiltering="true"
                QueryOnlyVisibleColumns="true"
                AllowSortingColumnPicker="true">
    <Columns>
        <RadzenDataGridColumn Property=@nameof(Employee.EmployeeID)
                              Title="ID"
                              Width="80px"
                              TextAlign="TextAlign.Center"
                              Frozen="true" />
        <RadzenDataGridColumn Property="@nameof(Employee.Photo)" Title="Photo" Sortable="false" Filterable="false" Width="200px" Pickable="false">
            <Template Context="data">
                <RadzenImage Path="@data.Photo" class="rz-gravatar" AlternateText="@(data.FirstName + " " + data.LastName)" />
            </Template>
        </RadzenDataGridColumn>
        <RadzenDataGridColumn Property=@nameof(Employee.FirstName) Title="First Name" />
        <RadzenDataGridColumn Property=@nameof(Employee.LastName) Title="Last Name" Width="150px" />
        <RadzenDataGridColumn Property=@nameof(Employee.Title) Title="Title" />
        <RadzenDataGridColumn Property=@nameof(Employee.TitleOfCourtesy) Title="Title Of Courtesy" />
        <RadzenDataGridColumn Property=@nameof(Employee.BirthDate) Title="Birth Date" FormatString="{0:d}" />
        <RadzenDataGridColumn Property=@nameof(Employee.HireDate) Title="Hire Date" FormatString="{0:d}" />
        <RadzenDataGridColumn Property=@nameof(Employee.Address) Title="Address" />
        <RadzenDataGridColumn Property=@nameof(Employee.City) Title="City" />
        <RadzenDataGridColumn Property=@nameof(Employee.Region) Title="Region" />
        <RadzenDataGridColumn Property=@nameof(Employee.PostalCode) Title="Postal Code" />
        <RadzenDataGridColumn Property=@nameof(Employee.Country) Title="Country" />
        <RadzenDataGridColumn Property=@nameof(Employee.HomePhone) Title="Home Phone" />
        <RadzenDataGridColumn Property=@nameof(Employee.Extension) Title="Extension" ColumnPickerTitle="Phone Number Extension" />
        <RadzenDataGridColumn Property=@nameof(Employee.Notes) Title="Notes" />
    </Columns>
</RadzenDataGrid>
<EventConsole @ref=@console />

@code {
    IEnumerable<Employee> employees;
    EventConsole console;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        employees = dbContext.Employees;
    }

    /// <summary>
    /// Only used to print the changes to the console.
    /// </summary>
    void PickedColumnsChanged(DataGridPickedColumnsChangedEventArgs<Employee> args)
    {
        console.Log($"Picked columns: {string.Join(", ", args.Columns.Select(c => c.Title))}");
    }
}
```
