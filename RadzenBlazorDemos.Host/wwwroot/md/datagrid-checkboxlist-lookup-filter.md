# DataGrid: CheckBoxList with Lookup

Drive the CheckBoxList filter from a lookup data source: filter by id while showing and searching by name.

Keywords: checkboxlist, lookup, filter, filtering, datagrid, table, dataview

> API reference: [RadzenDataGrid API](https://blazor.radzen.com/api/datagrid.md)

## Examples

## DataGrid CheckBoxList Filter with Lookup Data

Show friendly checkbox filter values in a Blazor DataGrid - use FilterLookupData to populate the checkbox list from a lookup, displaying names while filtering by IDs.

```razor
<RadzenDataGrid AllowFiltering="true" FilterMode="FilterMode.CheckBoxList" AllowPaging="true" PageSize="5" Data="@orders" ColumnWidth="200px">
    <Columns>
        <RadzenDataGridColumn Property="@nameof(Order.Reference)" Title="Reference" />
        <RadzenDataGridColumn Property="@nameof(Order.PriorityId)" Title="Priority"
                              FilterLookupData="@priorities" FilterLookupTextProperty="Name" FilterLookupValueProperty="Id">
            <Template Context="order">
                @priorityNames[order.PriorityId]
            </Template>
        </RadzenDataGridColumn>
    </Columns>
</RadzenDataGrid>

@code {
    class Order
    {
        public string Reference { get; set; } = "";
        public int PriorityId { get; set; }
    }

    record Priority(int Id, string Name);

    // Ids are not in alphabetical order of their names, so filtering/searching by name in the
    // dropdown is visibly different from the underlying id order.
    readonly Priority[] priorities =
    {
        new(1, "Urgent"),
        new(2, "Normal"),
        new(3, "Critical")
    };

    IReadOnlyDictionary<int, string> priorityNames;

    protected override void OnInitialized()
    {
        priorityNames = priorities.ToDictionary(p => p.Id, p => p.Name);
    }

    readonly List<Order> orders = new()
    {
        new Order { Reference = "ORD-001", PriorityId = 1 },
        new Order { Reference = "ORD-002", PriorityId = 2 },
        new Order { Reference = "ORD-003", PriorityId = 3 },
        new Order { Reference = "ORD-004", PriorityId = 2 },
        new Order { Reference = "ORD-005", PriorityId = 1 },
        new Order { Reference = "ORD-006", PriorityId = 3 }
    };
}
```
