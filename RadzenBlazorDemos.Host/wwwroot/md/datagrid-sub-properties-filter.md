# DataGrid: Filtering sub properties

This example demonstrates how to use sub properties in the RadzenDataGrid column filter.

Keywords: filter, sub properties, grid, datagrid, table

> API reference: [RadzenDataGrid API](https://blazor.radzen.com/api/datagrid.md)

## Examples

## DataGrid Sub Properties Column Filter

Filter Blazor DataGrid columns bound to nested properties - target sub-properties like Customer.Country using a property path so filtering works on related data.

```razor
@inherits DbContextPage

<RadzenDataGrid @ref=grid AllowPaging="true" AllowSorting="true" AllowFiltering="true" Data="@orders">
    <Columns>
        <RadzenDataGridColumn Property="Customer.CompanyName" Title="Company Name" />
        <RadzenDataGridColumn Property="OrderDetails" FilterProperty="Product.ProductName" Title="Product Name" Type="typeof(IEnumerable<OrderDetail>)" Sortable="false">
            <Template>
                @(string.Join(',', context.OrderDetails.Select(od => od.Product.ProductName)))
            </Template>
        </RadzenDataGridColumn>
    </Columns>
</RadzenDataGrid>

@code {
    RadzenDataGrid<Order> grid;
    IQueryable<Order> orders;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        orders = dbContext.Orders.Include("OrderDetails.Product").Include("Customer");
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            var column = grid.ColumnsCollection.Where(c => c.Property == "OrderDetails").FirstOrDefault();

            if (column != null)
            {
                column.SetFilterValue("Tofu");
                column.SetFilterOperator(FilterOperator.Contains);
                await grid.Reload();
            }
        }

        await base.OnAfterRenderAsync(firstRender);
    }
}
```
