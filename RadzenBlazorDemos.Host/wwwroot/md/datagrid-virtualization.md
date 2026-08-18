# DataGrid: IQueryable support

Virtualization allows you to render large amounts of data on demand. The RadzenDataGrid component uses Entity Framework to query the visible data.

Keywords: datagrid, bind, load, data, virtualization, ondemand

> API reference: [RadzenDataGrid API](https://blazor.radzen.com/api/datagrid.md)

## Examples

## DataGrid Virtualization

Virtualization allows you to render large amounts of data on demand. The RadzenDataGrid component uses Entity Framework to query the visible data.

```razor
@inherits DbContextPage

<RadzenDataGrid Data="@orderDetails" AllowVirtualization="true" Style="height:400px"
                AllowFiltering="true" FilterPopupRenderMode="PopupRenderMode.OnDemand" FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive" LogicalFilterOperator="LogicalFilterOperator.Or"
                AllowSorting="true">
    <Columns>
        <RadzenDataGridColumn Property="@nameof(OrderDetail.OrderID)" Title="OrderID" />
        <RadzenDataGridColumn Property="@nameof(OrderDetail.ProductID)" Title="ProductID" />
        <RadzenDataGridColumn Property="@nameof(OrderDetail.UnitPrice)" Title="Unit Price">
            <Template Context="detail">
                @String.Format(new System.Globalization.CultureInfo("en-US"), "{0:C}", detail.UnitPrice)
            </Template>
        </RadzenDataGridColumn>
        <RadzenDataGridColumn Property="@nameof(OrderDetail.Quantity)" Title="Quantity" />
        <RadzenDataGridColumn Property="@nameof(OrderDetail.Discount)" Title="Discount">
            <Template Context="detail">
                @String.Format("{0}%", detail.Discount * 100)
            </Template>
        </RadzenDataGridColumn>
    </Columns>
</RadzenDataGrid>

@code {
    IEnumerable<OrderDetail> orderDetails;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        orderDetails = dbContext.OrderDetails;
    }
}
```
