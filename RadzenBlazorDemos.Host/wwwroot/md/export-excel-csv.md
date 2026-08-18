# DataGrid: Export to Excel and CSV

This example demonstrates how to export a Radzen Blazor DataGrid to Excel and CSV.

Keywords: export, excel, csv

> API reference: [RadzenDataGrid API](https://blazor.radzen.com/api/datagrid.md)

## Examples

## Export to Excel and CSV

Export a Blazor DataGrid to Excel and CSV - download the data, respecting the current sort, filter, and column order.

```razor
@inherits DbContextPage

<RadzenDataGrid @ref="grid" AllowColumnPicking="true" AllowFiltering="true" AllowPaging="true" AllowSorting="true" Data="@orderDetails">
    <HeaderTemplate>
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem;" Wrap="FlexWrap.Wrap">
            <RadzenButton Text="Export XLS" Icon="grid_on" Click="@(args => Export("excel"))" />
            <RadzenButton Text="Export CSV" Icon="wrap_text" Click="@(args => Export("csv"))" />
        </RadzenStack>
    </HeaderTemplate>
    <Columns>
        <RadzenDataGridColumn Property="Order.OrderDate" Title="Order" FormatString="{0:d}" />
        <RadzenDataGridColumn Property="Product.ProductName" Title="Product" />
        <RadzenDataGridColumn Property="@nameof(OrderDetail.UnitPrice)" Title="Unit Price">
            <Template Context="detail">
                @String.Format(new System.Globalization.CultureInfo("en-US"), "{0:C}", detail.UnitPrice)
            </Template>
        </RadzenDataGridColumn>
        <RadzenDataGridColumn Property="@nameof(OrderDetail.Quantity)" Title="Quantity" />
        <RadzenDataGridColumn Property="@nameof(OrderDetail.Discount)" Title="Discount   " FormatString="{0:P}" />
    </Columns>
</RadzenDataGrid>

@code {
    RadzenDataGrid<OrderDetail> grid;

    IEnumerable<OrderDetail> orderDetails;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        orderDetails = dbContext.OrderDetails.Include("Order").Include("Product").ToList();
    }

    public void Export(string type)
    {
        var columnsToExport = grid.ColumnsCollection.Where(c => c.GetVisible() && !string.IsNullOrEmpty(c.Property));

        service.Export("OrderDetails", type, new Query() 
            { 
                OrderBy = grid.Query.OrderBy,
                Filter = grid.Query.Filter,
                Expand = "Order,Product",
                Select = string.Join(",", grid.ColumnsCollection.Where(c => c.GetVisible() && !string.IsNullOrEmpty(c.Property))
                            .Select(c => c.Property.Contains(".") ? $"{c.Property} as {c.Property.Replace(".", "_")}" : c.Property))
            });
    }
}
```
