# Card

The Blazor Card is a surface for grouping content, with variants, shadow, and customizable padding.

Keywords: card, container

> API reference: [RadzenCard API](https://blazor.radzen.com/api/card.md)

## Examples

## Blazor Card

The Blazor Card is a surface for grouping content, with variants, shadow, and customizable padding.

```razor
@inherits DbContextPage

<RadzenCard class="rz-my-12 rz-mx-auto" Style="max-width: 420px">
    <RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Start" Gap="1rem" class="rz-p-4">
        <RadzenImage Path="@order?.Employee?.Photo" Style="width: 100px; height: 100px; border-radius: 50%;" />
        <RadzenStack Gap="0">
            <RadzenText TextStyle="TextStyle.Overline" class="rz-display-flex rz-mt-2 rz-my-0">Employee</RadzenText>
            <RadzenText TextStyle="TextStyle.Body1"><b>@(order?.Employee?.FirstName + " " + order?.Employee?.LastName)</b></RadzenText>
            <RadzenText TextStyle="TextStyle.Overline" class="rz-display-flex rz-mt-4 rz-mb-0">Company</RadzenText>
            <RadzenText TextStyle="TextStyle.Body1"><b>@order?.Customer?.CompanyName</b></RadzenText>
        </RadzenStack>
    </RadzenStack>
    <RadzenCard class="rz-background-color-primary-light rz-shadow-0 rz-border-radius-0 rz-p-8" style="margin: 1rem calc(-1 * var(--rz-card-padding));">
        <RadzenText TextStyle="TextStyle.H6" TagName="TagName.P" class="rz-color-on-primary-light"><strong>Delivery Information</strong></RadzenText>
        <RadzenRow RowGap="0">
            <RadzenColumn SizeSM="4">
                <RadzenText TextStyle="TextStyle.Overline" class="rz-color-on-primary-light rz-display-flex rz-mt-4 rz-mb-0">Country</RadzenText>
                <RadzenText TextStyle="TextStyle.Body1" class="rz-color-on-primary-light"><b>@(order?.ShipCountry)</b></RadzenText>
                <RadzenText TextStyle="TextStyle.Overline" class="rz-color-on-primary-light rz-display-flex rz-mt-4 rz-mb-0">City</RadzenText>
                <RadzenText TextStyle="TextStyle.Body1" class="rz-color-on-primary-light"><b>@(order?.ShipCity)</b></RadzenText>
            </RadzenColumn>
            <RadzenColumn SizeSM="8">
                <RadzenText TextStyle="TextStyle.Overline" class="rz-color-on-primary-light rz-display-flex rz-mt-4 rz-mb-0">Ship name</RadzenText>
                <RadzenText TextStyle="TextStyle.Body1" class="rz-color-on-primary-light"><b>@(order?.ShipName)</b></RadzenText>
                <RadzenText TextStyle="TextStyle.Overline" class="rz-color-on-primary-light rz-display-flex rz-mt-4 rz-mb-0">Freight</RadzenText>
                <RadzenText TextStyle="TextStyle.Body1" class="rz-color-on-primary-light"><b>@String.Format(new System.Globalization.CultureInfo("en-US"), "{0:C}", order?.Freight)</b></RadzenText>
            </RadzenColumn>
        </RadzenRow>
    </RadzenCard>
    <RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.End" Gap="0">
        <RadzenButton Variant="Variant.Text" class="rz-text-secondary-color" Text="Cancel" />
        <RadzenButton Variant="Variant.Text" Text="Send" />
    </RadzenStack>
</RadzenCard>

@code {
    Order order;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        order = dbContext.Orders.Include("Customer").Include("Employee").FirstOrDefault();
    }
}
```


### Card Variant

Use the `Variant` property, e.g. `Variant="Variant.Outlined"`.

```razor
@inherits DbContextPage
<div class="rz-p-0 rz-p-md-12">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-4 rz-mb-6 rz-border-radius-1" Style="border: var(--rz-grid-cell-border);">
        <RadzenLabel Text="Variant:" />
        <RadzenSelectBar @bind-Value="@variant" TextProperty="Text" ValueProperty="Value" Data="@(Enum.GetValues(typeof(Variant)).Cast<Variant>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" class="rz-display-none rz-display-xl-flex" />
        <RadzenDropDown @bind-Value="@variant" TextProperty="Text" ValueProperty="Value" Data="@(Enum.GetValues(typeof(Variant)).Cast<Variant>().Select(t => new { Text = $"{t}", Value = t }))" class="rz-display-inline-flex rz-display-xl-none" />
    </RadzenStack>

    <RadzenCard Variant="@variant" class="rz-my-12 rz-mx-auto" Style="max-width: 420px">
        <RadzenStack Orientation="Orientation.Horizontal" JustifyContent="JustifyContent.Start" Gap="1rem" class="rz-p-4">
            <RadzenImage Path="@order?.Employee?.Photo" Style="width: 100px; height: 100px; border-radius: 50%;" />
            <RadzenStack Gap="0">
                <RadzenText TextStyle="TextStyle.Overline" class="rz-display-flex rz-mt-2 rz-my-0">Employee</RadzenText>
                <RadzenText TextStyle="TextStyle.Body1"><b>@(order?.Employee?.FirstName + " " + order?.Employee?.LastName)</b></RadzenText>
                <RadzenText TextStyle="TextStyle.Overline" class="rz-display-flex rz-mt-4 rz-mb-0">Company</RadzenText>
                <RadzenText TextStyle="TextStyle.Body1"><b>@order?.Customer?.CompanyName</b></RadzenText>
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>
</div>

@code {
    Order order;

    Variant variant = Variant.Filled;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        order = dbContext.Orders.Include("Customer").Include("Employee").FirstOrDefault();
    }
}
```
