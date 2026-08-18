# DataGrid: Pager Position

Set the pager position to Top, Bottom, or TopAndBottom.

Keywords: pager, paging, datagrid, table, dataview

> API reference: [RadzenDataGrid API](https://blazor.radzen.com/api/datagrid.md)

## Examples

## DataGrid Pager Position

Position the Blazor DataGrid pager where it fits your layout - at the top, the bottom, or both.

```razor
@inherits DbContextPage

<RadzenStack Gap="1rem">
    <RadzenCard Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem;" Wrap="FlexWrap.Wrap">
            <RadzenLabel Text="Pager Position:" Component="DropDown1" />
            <RadzenDropDown @bind-Value="@pagerPosition" TextProperty="Text" Name="DropDown1" ValueProperty="Value" 
                            Data="@(Enum.GetValues(typeof(PagerPosition)).Cast<PagerPosition>().Select(t => new { Text = $"{t}", Value = t }))" />
        </RadzenStack>
    </RadzenCard>

    <RadzenDataGrid Data="@orderDetails" PagerPosition="@pagerPosition" AllowPaging="true" AllowSorting="true">
        <Columns>
            <RadzenDataGridColumn Property="OrderID" Title="OrderID" />
            <RadzenDataGridColumn Property="ProductID" Title="ProductID" />
            <RadzenDataGridColumn Property="UnitPrice" Title="Unit Price">
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
</RadzenStack>

@code {
    PagerPosition pagerPosition = PagerPosition.Bottom;

    IEnumerable<OrderDetail> orderDetails;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        orderDetails = dbContext.OrderDetails;
    }
}
```
