# DataGrid: CheckBoxList Auto-Apply

Apply CheckBoxList column filters immediately as options are selected, without the Apply button.

Keywords: checkboxlist, auto, apply, filter, filtering, datagrid, table, dataview

> API reference: [RadzenDataGrid API](https://blazor.radzen.com/api/datagrid.md)

## Examples

## DataGrid CheckBoxList Filter Auto-Apply

Apply Blazor DataGrid checkbox filters instantly - turn on AutoApplyCheckBoxListFilter so selections filter the grid immediately, with no Apply click.

```razor
<RadzenDataGrid AllowFiltering="true" FilterMode="FilterMode.CheckBoxList" AutoApplyCheckBoxListFilter="true"
                AllowPaging="true" PageSize="5" Data="@orders" ColumnWidth="200px">
    <Columns>
        <RadzenDataGridColumn Property="@nameof(Order.Reference)" Title="Reference" />
        <RadzenDataGridColumn Property="@nameof(Order.Status)" Title="Status" />
    </Columns>
</RadzenDataGrid>

@code {
    class Order
    {
        public string Reference { get; set; } = "";
        public string Status { get; set; } = "";
    }

    readonly List<Order> orders = new()
    {
        new Order { Reference = "ORD-001", Status = "Open" },
        new Order { Reference = "ORD-002", Status = "Shipped" },
        new Order { Reference = "ORD-003", Status = "Closed" },
        new Order { Reference = "ORD-004", Status = "Open" },
        new Order { Reference = "ORD-005", Status = "Shipped" },
        new Order { Reference = "ORD-006", Status = "Closed" }
    };
}
```
