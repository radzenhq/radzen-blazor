# DropDown: Single selection

Free Blazor DropDown (select) component with data binding, filtering, multiple selection, grouping, templates, and virtualization for large lists. Bind to any IEnumerable or IQueryable.

Keywords: select, picker, form, edit, dropdown, combobox, multiselect

> API reference: [RadzenDropDown API](https://blazor.radzen.com/api/dropdown.md)

## Examples

## Blazor DropDown

The Radzen Blazor DropDown lets users pick a value from a list - a select / combobox with data binding, filtering, multiple selection, grouping, templates, and virtualization for large lists.

### Get and Set the value of DropDown

As all Radzen Blazor input components the DropDown has a Value property which gets and sets the value of the component. Use `@-Value` to get the user input.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Select Value" Component="DropDownBindValue" />
    <RadzenDropDown @bind-Value=@value Data=@companyNames Style="width: 100%; max-width: 400px;" Name="DropDownBindValue" />
</RadzenStack>

@code {
    string value = "Around the Horn";
    IEnumerable<string> companyNames;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        companyNames = dbContext.Customers.Select(c => c.CompanyName).Distinct();
    }
}
```


### Get and Set the value of DropDown using Value and Change event

Value property can be used to set the value of the component and `Change` event to get the user input.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Select Value" Component="DropDownChangeEvent" />
    <RadzenDropDown TValue="string" Value=@value Data=@companyNames Change="@(args => value = $"{args}")" Style="width: 100%; max-width: 400px;" Name="DropDownChangeEvent" />
</RadzenStack>

@code {
    string value = "Around the Horn";
    IEnumerable<string> companyNames;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        companyNames = dbContext.Customers.Select(c => c.CompanyName).Distinct();
    }
}
```


### Define Text and Value properties

Use the `TextProperty` and `ValueProperty` properties to specify which fields to display and use as values.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Select Value" Component="DropDownTextValueProperties" />
    <RadzenDropDown @bind-Value=@value Data=@customers TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)" Style="width: 100%; max-width: 400px;" Name="DropDownTextValueProperties" />
</RadzenStack>

@code {
    string value = "AROUT";
    IEnumerable<Customer> customers;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        customers = dbContext.Customers;
    }
}
```


### DropDown with templates

Use the `Template`,`HeaderTemplate` and `FooterTemplate` properties to customize the dropdown list.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Select Value" Component="DropDownTemplate" />
    <RadzenDropDown @bind-Value=@value Data=@customers TextProperty="@nameof(Customer.CompanyName)" ValueProperty="@nameof(Customer.CustomerID)" Style="width: 400px;" Name="DropDownTemplate">
        <HeaderTemplate>
            <b>Select a Customer</b>
        </HeaderTemplate>
        <Template>
            Company: @((context as Customer).CompanyName)
        </Template>
        <ValueTemplate>
            Company: @((context as Customer).CompanyName)
        </ValueTemplate>
        <FooterTemplate>
            <i>Total Customers: @customers.Count()</i>
        </FooterTemplate>
    </RadzenDropDown>
</RadzenStack>

@code {
    string value = "AROUT";
    IEnumerable<Customer> customers;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        customers = dbContext.Customers;
    }
}
```


### Disable specific item

Use the `IsDisabled` parameter of `RadzenDropDownItem` to disable specific items in the dropdown.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Select Value" Component="DropDownDisabledItem" />
    <RadzenDropDown @bind-Value=@value Data=@products TextProperty="@nameof(Product.ProductName)" ValueProperty="@nameof(Product.ProductID)" DisabledProperty="Discontinued" Style="width: 100%; max-width: 400px;" Name="DropDownDisabledItem" />
</RadzenStack>

@code {
    int value = 1;
    IEnumerable<Product> products;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        products = dbContext.Products;
    }
}
```


### Clear selected item

Use the `AllowClear` property to enable a clear button that allows users to deselect the current selection.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Select Value" Component="DropDownClear" />
    <RadzenDropDown AllowFiltering=true @bind-Value=@value Data=@products TextProperty="@nameof(Product.ProductName)" ValueProperty="ProductID" AllowClear=true Placeholder="Select product" Style="width: 100%; max-width: 400px;" Name="DropDownClear" />
</RadzenStack>

@code {
    int? value;
    IEnumerable<Product> products;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        products = dbContext.Products;
    }
}
```


### Editable DropDown

Use the `ValueTemplate` with an embedded input bound to the same value to let users edit the selected item in place. Set `ShowValueTemplateOnEmpty` to `true` to render the template (and its editor) even when no item is selected, so the user can type an initial value without first picking from the list. The pattern requires the DropDown `Value` to be the displayed text itself (e.g. a primitive type) and is not designed for use with `TextProperty`/`ValueProperty`.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel id="DropDownEditLabel" Text="Select or type a value" Component="DropDownEdit" />
    <RadzenDropDown @bind-Value=@value Data=@companyNames ShowValueTemplateOnEmpty="true" Style="width: 100%; max-width: 400px;" Name="DropDownEdit">
        <ValueTemplate>
            <RadzenTextBox @bind-Value=@value Style="width:120%; height:120%; margin:-15px" aria-labelledby="DropDownEditLabel" />
        </ValueTemplate>
    </RadzenDropDown>
</RadzenStack>

@code {
    string value = "";
    IEnumerable<string> companyNames;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        companyNames = dbContext.Customers.Select(c => c.CompanyName).Distinct();
    }
}
```


### Open and close events

Handle the `Open` and `Close` events to respond when the dropdown popup is opened or closed.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel Text="Select Value" Component="DropDownChangeEvent" />
    <RadzenDropDown TValue="string" Value=@value Data=@companyNames Open="@(() => Console.WriteLine("opened"))" Close="@(()=>Console.WriteLine("closed"))" Style="width: 100%; max-width: 400px;" Name="DropDownChangeEvent" />
</RadzenStack>

@code {
    string value = "Around the Horn";
    IEnumerable<string> companyNames;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        companyNames = dbContext.Customers.Select(c => c.CompanyName).Distinct();
    }
}
```


### DropDown Sizes

Use the `InputSize` property to set the DropDown size. Available sizes are ExtraSmall, Small, Medium (default), and Large.

```razor
<RadzenStack Gap="1rem" class="rz-p-sm-12">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Large" Style="width: 80px;" />
        <RadzenDropDown @bind-Value=@value Data=@items InputSize="InputSize.Large" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Medium" Style="width: 80px;" />
        <RadzenDropDown @bind-Value=@value Data=@items InputSize="InputSize.Medium" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Small" Style="width: 80px;" />
        <RadzenDropDown @bind-Value=@value Data=@items InputSize="InputSize.Small" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Extra Small" Style="width: 80px;" />
        <RadzenDropDown @bind-Value=@value Data=@items InputSize="InputSize.ExtraSmall" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
</RadzenStack>

@code {
    string value = "Item 1";
    IEnumerable<string> items = new[] { "Item 1", "Item 2", "Item 3" };
}
```
