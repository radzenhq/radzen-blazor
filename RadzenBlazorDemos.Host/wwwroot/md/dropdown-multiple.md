# DropDown: Multiple selection

Select multiple items from the Blazor DropDown (multiselect). Bind to a collection, show a summary label, and set an equality comparer when binding to objects.

Keywords: select, picker, form, edit, multiple, dropdown, multiselect

> API reference: [RadzenDropDown API](https://blazor.radzen.com/api/dropdown.md)

## Examples

## DropDown multiple selection

Select multiple items from the Blazor DropDown (multiselect). Bind to a collection, show a summary label for the selection, and set an equality comparer when binding to objects.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Select Values" Component="DropDownMultiple" />
    <RadzenDropDown @bind-Value=@values Data=@products TextProperty="@nameof(Product.ProductName)" ValueProperty="@nameof(Product.ProductID)" Name="DropDownMultiple"
                    Multiple=true AllowClear=true Placeholder="Select products" Style="width: 100%; max-width: 400px;" />
</RadzenStack>

@code {
    IEnumerable<int> values = new int[] { 1, 2 };
    IEnumerable<Product> products;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        products = dbContext.Products;
    }
}
```


### DropDown multiple selection with chips

Use `Chips="true"` to display selected items as removable chips in the dropdown.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Select Values" Component="DropDownMultipleChips" />
    <RadzenDropDown @bind-Value=@values Data=@products TextProperty="@nameof(Product.ProductName)" ValueProperty="@nameof(Product.ProductID)" Name="DropDownMultipleChips"
        Multiple=true AllowClear=true Placeholder="Select products" Chips=true Style="width: 100%; max-width: 400px;" />
</RadzenStack>

@code {
    IList<int> values = new int[] { 1, 2 };
    IEnumerable<Product> products;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        products = dbContext.Products;
    }
}
```


### Define max labels and selected items text

Use `MaxSelectedLabels` to limit displayed items and `SelectedItemsText` to show a custom message when many items are selected.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Select Values" Component="DropDownMultipleMaxLabels" />
    <RadzenDropDown @bind-Value=@values Data=@products TextProperty="@nameof(Product.ProductName)" ValueProperty="@nameof(Product.ProductID)" Name="DropDownMultipleMaxLabels"
                    Multiple=true AllowClear=true Placeholder="Select products" 
                    MaxSelectedLabels="2" SelectAllText="Select all items" SelectedItemsText="are now selected" Style="width: 100%; max-width: 400px;" />
</RadzenStack>

@code {
    IEnumerable<int> values = new int[] { 1, 2 };
    IEnumerable<Product> products;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        products = dbContext.Products;
    }
}
```


### Specify an Equality Comparer for item selection. Useful when binding directly to an object collection.

Use `EqualityComparer` to define custom comparison logic for determining when items are equal.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Select Values" Component="DropDownMultiple" />
    <RadzenDropDown @bind-Value=@values Data=@products TextProperty="@nameof(Product.ProductName)" Name="DropDownMultiple"
                    Multiple=true AllowClear=true Placeholder="Select products" Style="width: 100%; max-width: 400px;"
                    ItemComparer="@Product.Comparer"/>
</RadzenStack>

@code {
    IEnumerable<Product> values = [];
    IEnumerable<Product> products;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        products = dbContext.Products;
        values =
        [
            new Product { ProductName = "Chai" },
            new Product { ProductName = "Aniseed Syrup" }
        ];
    }
}
```
