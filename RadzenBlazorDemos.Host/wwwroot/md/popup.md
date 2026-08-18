# Popup

The Blazor Popup shows floating content anchored to an element via PopupService, for custom dropdowns and overlays.

Keywords: popup, dropdown

> API reference: [RadzenPopup API](https://blazor.radzen.com/api/popup.md)

## Examples

## Blazor Popup

The Blazor Popup shows floating content anchored to an element via PopupService, for custom dropdowns and overlays.

```razor
@inherits DbContextPage

<style type="text/css">
    .my-popup {
        display: none;
        position: absolute;
        overflow: hidden;
        height: 360px;
        width: 600px;
        border: var(--rz-panel-border);
        background-color: var(--rz-panel-background-color);
        box-shadow: var(--rz-panel-shadow);
        border-radius: var(--rz-border-radius)
    }
 </style>

<div class="rz-p-12 rz-text-align-center">
    <RadzenButton @ref=button Text="@(orderId != null ? "Selected order: " + orderId.ToString() : "Select order")" Click="@(args => popup.ToggleAsync(button.Element))" />
</div>

<RadzenPopup @ref=popup Lazy=true class="my-popup">
    <RadzenStack Orientation="Orientation.Vertical" Gap="1rem" class="rz-h-100 rz-p-4">
    <RadzenTextBox id="search" Placeholder="Type to search..." @oninput=@(args => searchString = $"{args.Value}") Value="@searchString" />
    <RadzenDataList @ref=dataList AllowVirtualization=true Data="@orders" Style="flex: 1; --rz-datalist-padding: 0.5rem 0; --rz-datalist-item-margin-inline: 0; overflow:auto;">
        <Template Context="order">
            <RadzenRow>
                <RadzenColumn Size="8" class="rz-text-truncate">
                    <RadzenBadge BadgeStyle="BadgeStyle.Light" Text=@($"{order.OrderID}") class="rz-me-1" />
                    <b>@(order.ShipName)</b>
                </RadzenColumn>
                <RadzenColumn Size="4" class="rz-text-align-end">
                    <RadzenBadge BadgeStyle="BadgeStyle.Success" Text=@($"{String.Format(new System.Globalization.CultureInfo("en-US"), "{0:C}", order.Freight)}") />
                </RadzenColumn>
            </RadzenRow>
            <hr style="border: none; background-color: var(--rz-text-disabled-color); height: 1px; margin: 1rem 0;" />
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.SpaceBetween" Gap="1rem">
                <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="1rem">
                    <RadzenImage Path="@order.Employee?.Photo" Style="width: 80px; height: 80px; border-radius: 50%" AlternateText="@(order.Employee?.FirstName + " " + order.Employee?.LastName)" />
                    <RadzenStack Gap="0">
                        <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P" class="rz-mb-0">@(order.Employee?.FirstName + " " + order.Employee?.LastName)</RadzenText>
                        <RadzenText TextStyle="TextStyle.Body1">@order.ShipAddress</RadzenText>
                        <RadzenText TextStyle="TextStyle.Body2" class="rz-mb-0">@(order.ShipCity), @(order.ShipCountry)</RadzenText>
                    </RadzenStack>
                </RadzenStack>
                <RadzenButton Text="Select" Click="@(args => SelectOrder(order))" Visible=@(orderId != order.OrderID) />
            </RadzenStack>
        </Template>
    </RadzenDataList>
    </RadzenStack>
</RadzenPopup>

@code {
    RadzenButton button;
    RadzenPopup popup;
    RadzenDataList<Order> dataList;
    IEnumerable<Order> orders;
    int? orderId;
    string searchString = "";

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        orders = dbContext.Orders.Include("Customer").Include("Employee")
            .Where(o => o.OrderID.ToString().Contains(searchString)
                || o.Customer.CompanyName.ToLowerInvariant().Contains(searchString)
                    || o.Employee.FirstName.ToLowerInvariant().Contains(searchString)
                        || o.Employee.LastName.ToLowerInvariant().Contains(searchString)
                            || o.ShipCity.ToLowerInvariant().Contains(searchString)
                                || o.ShipCountry.ToLowerInvariant().Contains(searchString));
    }

    async Task SelectOrder(Order order)
    {
        orderId = order.OrderID; 
        await popup.CloseAsync();
    }
}
```


### Popup for TextBox with DataGrid.


```razor
@inherits DbContextPage

<style type="text/css">
    .my-popup {
        display: none;
        position: absolute;
        overflow: hidden;
        height: 360px;
        width: 600px;
        border: var(--rz-panel-border);
        background-color: var(--rz-panel-background-color);
        box-shadow: var(--rz-panel-shadow);
        border-radius: var(--rz-border-radius)
    }
 </style>

<div class="rz-p-12 rz-text-align-center">
    <RadzenTextBox @ref=textBox Value="@customerId" @oninput=@OnInput
                   @onclick="@(args => popup.ToggleAsync(textBox.Element))" @onkeydown=@OnKeyDown />
</div>

<RadzenPopup @ref=popup id="popup" AutoFocusFirstElement="false" class="my-popup">
    <RadzenDataGrid @ref=grid id="grid" TItem="Customer" Data="@customers" RowSelect="@OnRowSelect" AllowSorting="true" Style="height:360px">
        <Columns>
            <RadzenDataGridColumn Property="CustomerID" Title="CustomerID" />
            <RadzenDataGridColumn Property="CompanyName" Title="CompanyName" />
            <RadzenDataGridColumn Property="ContactName" Title="ContactName" />
            <RadzenDataGridColumn Property="City" Title="City" />
            <RadzenDataGridColumn Property="Country" Title="Country" />
        </Columns>
    </RadzenDataGrid>
</RadzenPopup>

@code {
    RadzenTextBox textBox;
    string value = "";
    string customerId;
    IEnumerable<Customer> customers;
    RadzenPopup popup;
    RadzenDataGrid<Customer> grid;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        customers = dbContext.Customers
            .Where(c => c.CustomerID.ToString().Contains(value)
                || c.CompanyName.ToLowerInvariant().Contains(value)
                    || c.ContactName.ToLowerInvariant().Contains(value)
                        || c.City.ToLowerInvariant().Contains(value)
                            || c.Country.ToLowerInvariant().Contains(value));
    }

    async Task OnRowSelect(Customer customer)
    {
        value = "";
        customerId = customer.CustomerID;
        await popup.CloseAsync();
    }

    async Task OnInput(ChangeEventArgs args)
    {
        selectedIndex = 0;
        customerId = value = $"{args.Value}";
        await grid.Reload();
    }

    int selectedIndex = 0;
    async Task OnKeyDown(KeyboardEventArgs args)
    {
        var items = customers;
        var popupOpened = await JSRuntime.InvokeAsync<bool>("Radzen.popupOpened", "popup");

        var key = args.Code != null ? args.Code : args.Key;

        if (!args.AltKey && (key == "ArrowDown" || key == "ArrowUp"))
        {
            var result = await JSRuntime.InvokeAsync<int[]>("Radzen.focusTableRow", "grid", key, selectedIndex, null, false);
            selectedIndex = result.First();
        }
        else if (args.AltKey && key == "ArrowDown" || key == "Enter" || key == "NumpadEnter")
        {
            if (popupOpened && (key == "Enter" || key == "NumpadEnter"))
            {
                customerId = items.ElementAtOrDefault(selectedIndex)?.CustomerID;
            }

            await popup.ToggleAsync(textBox.Element);
        }
        else if (key == "Escape" || key == "Tab")
        {
            await popup.CloseAsync();
        }
    }
}
```
