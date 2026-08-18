# GridLines

Deside where to display grid lines in your Blazor RadzenDataGrid.

Keywords: grid, lines, border, gridlines

> API reference: [RadzenGridLines API](https://blazor.radzen.com/api/gridlines.md)

## Examples

## DataGrid GridLines

Control Blazor DataGrid grid lines - show horizontal, vertical, both, or no borders to match your design.

```razor
@inherits DbContextPage

<RadzenCard Variant="Variant.Outlined" class="rz-my-4">
    <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center">
        <div>GridLines:</div>
        <RadzenSelectBar @bind-Value="@GridLines" TextProperty="Text" ValueProperty="Value"
                         Data="@(Enum.GetValues(typeof(Radzen.DataGridGridLines)).Cast<Radzen.DataGridGridLines>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" />
    </RadzenStack>
</RadzenCard>

<RadzenDataGrid Data="@orderDetails" GridLines="@GridLines" AllowPaging="true" AllowSorting="true">
    <Columns>
        <RadzenDataGridColumn Property="OrderID" Title="OrderID" />
        <RadzenDataGridColumn Property="ProductID" Title="ProductID" />
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
    Radzen.DataGridGridLines GridLines = Radzen.DataGridGridLines.Default;

    IEnumerable<OrderDetail> orderDetails;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        orderDetails = dbContext.OrderDetails;
    }
}
```
