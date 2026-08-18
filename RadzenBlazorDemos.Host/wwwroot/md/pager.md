# Pager

Demonstration and configuration of the Radzen Blazor Pager component.

Keywords: pager, paging

> API reference: [RadzenPager API](https://blazor.radzen.com/api/pager.md)

## Examples

## Blazor Pager

Configurable page sizes and density options.

```razor
@inherits DbContextPage

<RadzenDataList WrapItems="true" AllowPaging="false" Data="@orders" TItem="Order">
    <Template Context="order">
        <RadzenCard Style="width:300px;">
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center">
                <RadzenImage Path="@order.Employee?.Photo" Style="width: 80px; height: 80px; border-radius: 50%" AlternateText="@(order.Employee?.FirstName + " " + order.Employee?.LastName)" />
                <RadzenStack Gap="0">
                    <RadzenText TextStyle="TextStyle.Overline" class="rz-display-flex rz-mt-2 rz-my-0">Employee</RadzenText>
                    <RadzenText TextStyle="TextStyle.Body1"><b>@(order?.Employee?.FirstName + " " + order?.Employee?.LastName)</b></RadzenText>
                    <RadzenText TextStyle="TextStyle.Overline" class="rz-display-flex rz-mt-4 rz-mb-0">City</RadzenText>
                    <RadzenText TextStyle="TextStyle.Body1"><b>@(order.ShipCity), @(order.ShipCountry)</b></RadzenText>
                </RadzenStack>
            </RadzenStack>
            <hr style="border: none; background-color: rgba(0,0,0,.2); height: 1px; margin: 1rem 0;" />
            <RadzenRow>
                <RadzenColumn Size="8" class="rz-text-truncate">
                    <b>@(order.ShipName)</b>
                </RadzenColumn>
                <RadzenColumn Size="4" class="rz-text-align-end">
                    <RadzenBadge BadgeStyle="BadgeStyle.Secondary" Text=@($"{String.Format(new System.Globalization.CultureInfo("en-US"), "{0:C}", order.Freight)}") />
                </RadzenColumn>
            </RadzenRow>
        </RadzenCard>
    </Template>
</RadzenDataList>

<RadzenPager ShowPagingSummary="true" PagingSummaryFormat="@pagingSummaryFormat" HorizontalAlign="HorizontalAlign.Right" Count="count" PageSize="@pageSize" PageNumbersCount="5" PageChanged="@PageChanged" />

@code {
    string pagingSummaryFormat = "Displaying page {0} of {1} (total {2} records)";
    int pageSize = 6;
    int count;
    IEnumerable<Order> orders;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        count = dbContext.Orders.Count();
        orders = GetOrders(0, pageSize);
    }

    void PageChanged(PagerEventArgs args)
    {
        orders = GetOrders(args.Skip, args.Top);
    }

    IEnumerable<Order> GetOrders(int skip, int take)
    {
        return dbContext.Orders.Include("Customer").Include("Employee").Skip(skip).Take(take).ToList();
    }
}
```


### Allow Reload

Use the `AllowReload` property to show a reload button. Handle the `PageReload` event to refresh the current page data.

```razor
@inherits DbContextPage

<RadzenDataList WrapItems="true" AllowPaging="false" Data="@orders" TItem="Order">
    <Template Context="order">
        <RadzenCard Style="width:300px;">
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center">
                <RadzenImage Path="@order.Employee?.Photo" Style="width: 80px; height: 80px; border-radius: 50%" AlternateText="@(order.Employee?.FirstName + " " + order.Employee?.LastName)" />
                <RadzenStack Gap="0">
                    <RadzenText TextStyle="TextStyle.Overline" class="rz-display-flex rz-mt-2 rz-my-0">Employee</RadzenText>
                    <RadzenText TextStyle="TextStyle.Body1"><b>@(order?.Employee?.FirstName + " " + order?.Employee?.LastName)</b></RadzenText>
                    <RadzenText TextStyle="TextStyle.Overline" class="rz-display-flex rz-mt-4 rz-mb-0">City</RadzenText>
                    <RadzenText TextStyle="TextStyle.Body1"><b>@(order.ShipCity), @(order.ShipCountry)</b></RadzenText>
                </RadzenStack>
            </RadzenStack>
        </RadzenCard>
    </Template>
</RadzenDataList>

<RadzenPager AllowReload="true" ShowPagingSummary="true" PagingSummaryFormat="@pagingSummaryFormat" HorizontalAlign="HorizontalAlign.Right" Count="count" PageSize="@pageSize" PageNumbersCount="5" PageChanged="@PageChanged" PageReload="@OnReload" />

<RadzenText TextStyle="TextStyle.Body2" class="rz-mt-4">Reload count: @reloadCount</RadzenText>

@code {
    string pagingSummaryFormat = "Displaying page {0} of {1} (total {2} records)";
    int pageSize = 6;
    int count;
    int reloadCount;
    IEnumerable<Order> orders;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        count = dbContext.Orders.Count();
        orders = GetOrders(0, pageSize);
    }

    void PageChanged(PagerEventArgs args)
    {
        orders = GetOrders(args.Skip, args.Top);
    }

    void OnReload()
    {
        reloadCount++;
    }

    IEnumerable<Order> GetOrders(int skip, int take)
    {
        return dbContext.Orders.Include("Customer").Include("Employee").Skip(skip).Take(take).ToList();
    }
}
```


### Pager Density

Use the `Density` property to change the size and density of the pager - `Density="Density.Compact"`.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center" class="rz-p-4 rz-mb-6 rz-border-radius-1" Style="border: var(--rz-grid-cell-border);">
    <RadzenText TextStyle="TextStyle.Body1">Density:</RadzenText>
    <RadzenSelectBar @bind-Value="@Density" TextProperty="Text" ValueProperty="Value"
                    Data="@(Enum.GetValues(typeof(Density)).Cast<Density>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" />
</RadzenStack>

<RadzenPager Density="@Density" HorizontalAlign="HorizontalAlign.Center" Count="count" PageSize="@pageSize" PageNumbersCount="6" PageChanged="@PageChanged" />

<RadzenDataList WrapItems="true" AllowPaging="false" Data="@orders" TItem="Order">
    <Template Context="order">
        <RadzenCard Style="flex: 1;">
            <RadzenStack AlignItems="AlignItems.Center" Gap="1rem">
                <RadzenImage Path="@order.Employee?.Photo" Style="width: 80px; height: 80px; border-radius: 50%" AlternateText="@(order.Employee?.FirstName + " " + order.Employee?.LastName)" />
                <RadzenStack Gap="0">
                    <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.P" TextAlign="TextAlign.Center">@(order?.Employee?.FirstName + " " + order?.Employee?.LastName)</RadzenText>
                    <RadzenText TextStyle="TextStyle.Body2" TextAlign="TextAlign.Center">@(order.ShipCity), @(order.ShipCountry)</RadzenText>
                </RadzenStack>
            </RadzenStack>
        </RadzenCard>
    </Template>
</RadzenDataList>

<RadzenPager Density="@Density" HorizontalAlign="HorizontalAlign.Center" Count="count" PageSize="@pageSize" PageNumbersCount="6" PageChanged="@PageChanged" />

@code {
    Density Density = Density.Compact;
    int pageSize = 5;
    int count;
    IEnumerable<Order> orders;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        count = dbContext.Orders.Count();
        orders = GetOrders(0, pageSize);
    }

    void PageChanged(PagerEventArgs args)
    {
        orders = GetOrders(args.Skip, args.Top);
    }

    IEnumerable<Order> GetOrders(int skip, int take)
    {
        return dbContext.Orders.Include("Customer").Include("Employee").Skip(skip).Take(take).ToList();
    }
}
```
