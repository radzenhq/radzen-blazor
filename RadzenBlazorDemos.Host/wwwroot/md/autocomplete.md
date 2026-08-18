# AutoComplete

The Blazor AutoComplete suggests matching items as the user types, with templates, custom filter operators, and on-demand data loading.

Keywords: form, complete, suggest, edit

> API reference: [RadzenAutoComplete API](https://blazor.radzen.com/api/autocomplete.md)

## Examples

## Blazor AutoComplete

The Blazor AutoComplete suggests matching items as the user types, with templates, custom filter operators, and on-demand data loading.

### Get and Set the value of AutoComplete

As all Radzen Blazor input components the AutoComplete has a Value property which gets and sets the value of the component. Use `@-Value` to get the user input.

```razor
@inherits DbContextPage

<div class="rz-p-12 rz-text-align-center">
    <RadzenAutoComplete @bind-Value=@companyName Data=@customers TextProperty="@nameof(Customer.CompanyName)" Style="width: 13rem" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Company Name" }})" />
    <RadzenText TextStyle="TextStyle.Body2">Start typing e.g. France</RadzenText>
    @if (!string.IsNullOrEmpty(companyName))
    {
        <RadzenText TextStyle="TextStyle.Body2">Value is: <strong>@companyName</strong></RadzenText>
    }
</div>

@code {
    string companyName;

    IEnumerable<Customer> customers;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        customers = dbContext.Customers;
    }
}
```


### Get and Set the value of AutoComplete using Value and Change event

Value property can be used to set the value of the component and `Change` event to get the user input.

```razor
@inherits DbContextPage

<div class="rz-p-12 rz-text-align-center">
    <RadzenAutoComplete Value=@companyName Change=@OnChange Data=@customers TextProperty="@nameof(Customer.CompanyName)" Style="width: 13rem" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Company Name" }})" />
    <RadzenText TextStyle="TextStyle.Body2">Start typing e.g. France</RadzenText>
    @if (!string.IsNullOrEmpty(companyName))
    {
        <RadzenText TextStyle="TextStyle.Body2">Value is: <strong>@companyName</strong></RadzenText>
    }
</div>

@code {
    string companyName;

    IEnumerable<Customer> customers;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        customers = dbContext.Customers;
    }

    void OnChange(dynamic args)
    {
        companyName = args;
    }
}
```


### Get the selected item of AutoComplete

Use the `SelectedItem` property to get the currently selected item from the AutoComplete.

```razor
@inherits DbContextPage
<div class="rz-p-12 rz-text-align-center">
    <RadzenButton Text="Set the first item as selected" Click="@(args => selectedItem = customers.FirstOrDefault())" />
</div>
<div class="rz-p-12 rz-text-align-center">
    <RadzenAutoComplete @bind-Value=@companyName @bind-SelectedItem=@selectedItem
                        Data=@customers TextProperty="@nameof(Customer.CompanyName)"
                        Style="width: 13rem" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Company Name" }})" />
    <RadzenText TextStyle="TextStyle.Body2">Start typing e.g. France</RadzenText>
    @if (selectedItem != null)
    {
        <RadzenText TextStyle="TextStyle.Body2">CustomerID is: <strong>@(((Customer)selectedItem).CustomerID)</strong></RadzenText>
    }
    @if (!string.IsNullOrEmpty(companyName))
    {
        <RadzenText>Value is: <strong>@companyName</strong></RadzenText>
    }
</div>

@code {
    string companyName;
    object selectedItem;

    IEnumerable<Customer> customers;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        customers = dbContext.Customers;
    }
}
```


### Define AutoComplete placeholder

Use the `Placeholder` property to display a hint text when the AutoComplete is empty.

```razor
@inherits DbContextPage

<div class="rz-p-12 rz-text-align-center">
    <RadzenAutoComplete Placeholder="Select a customer..." @bind-Value=@companyName Data=@customers TextProperty="@nameof(Customer.CompanyName)" Style="width: 13rem" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Company Name" }})" />
    <RadzenText TextStyle="TextStyle.Body2">Start typing e.g. France</RadzenText>
    @if (!string.IsNullOrEmpty(companyName))
    {
        <RadzenText TextStyle="TextStyle.Body2">Value is: <strong>@companyName</strong></RadzenText>
    }
</div>

@code {
    string companyName;

    IEnumerable<Customer> customers;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        customers = dbContext.Customers;
    }
}
```


### Define AutoComplete template

Use the `Template` property to customize how items are displayed in the AutoComplete dropdown.

```razor
@inherits DbContextPage

<div class="rz-p-12 rz-text-align-center">
    <RadzenAutoComplete @bind-Value=@companyName Placeholder="Select a customer..." Data=@customers TextProperty="@nameof(Customer.CompanyName)" Style="width: 13rem" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Company Name" }})">
        <Template>
            CompanyName: @context.CompanyName
            <br />
            Country: @context.Country
        </Template>
    </RadzenAutoComplete>
   <RadzenText TextStyle="TextStyle.Body2">Start typing e.g. France</RadzenText>
    @if (!string.IsNullOrEmpty(companyName))
    {
        <RadzenText TextStyle="TextStyle.Body2">Value is: <strong>@companyName</strong></RadzenText>
    }
</div>

@code {
    string companyName;

    IEnumerable<Customer> customers;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        customers = dbContext.Customers;
    }
}
```


### Change AutoComplete filter operator, case sensitivity and delay

Show items that start with a specific string, case insensitive after 100ms

```razor
@inherits DbContextPage

<div class="rz-p-12 rz-text-align-center">
    <RadzenAutoComplete FilterOperator="StringFilterOperator.StartsWith" FilterDelay="100" FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
        @bind-Value=@companyName Data=@customers TextProperty="@nameof(Customer.CompanyName)" Style="width: 13rem" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Company Name" }})" />
    <RadzenText TextStyle="TextStyle.Body2">Start typing e.g. France</RadzenText>
    @if (!string.IsNullOrEmpty(companyName))
    {
        <RadzenText TextStyle="TextStyle.Body2">Value is: <strong>@companyName</strong></RadzenText>
    }
</div>

@code {
    string companyName;

    IEnumerable<Customer> customers;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        customers = dbContext.Customers;
    }
}
```


### Load data on-demand in AutoComplete and apply custom filter and sort

Use the `LoadData` event to load data on-demand and implement custom filtering and sorting logic.

```razor
@inherits DbContextPage

<div class="rz-p-12 rz-text-align-center">
    <RadzenAutoComplete @bind-Value=@companyName Data=@customers TextProperty="@nameof(Customer.CompanyName)" LoadData=@OnLoadData Style="width: 13rem" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Company Name" }})" />
    <RadzenText TextStyle="TextStyle.Body2">Start typing e.g. France</RadzenText>
    @if (!string.IsNullOrEmpty(companyName))
    {
        <RadzenText TextStyle="TextStyle.Body2">Value is: <strong>@companyName</strong></RadzenText>
    }
</div>

@code {
    string companyName;

    IEnumerable<Customer> customers;

    async Task OnLoadData(LoadDataArgs args)
    {
        customers = await Task.FromResult(dbContext.Customers
            .Where(c => c.CustomerID.Contains(args.Filter) || c.ContactName.Contains(args.Filter))
            .OrderBy(c => c.Country));
    }
}
```


### Empty and Loading templates

Use `EmptyTemplate` to customize the popup when there are no matching items and `LoadingTemplate` together with the `IsLoading` property to show a loading indicator while data is being fetched. `LoadingTemplate` takes precedence over the items and the `EmptyTemplate`.

```razor
@inherits DbContextPage

<div class="rz-p-12 rz-text-align-center">
    <RadzenAutoComplete @bind-Value=@companyName Data=@customers TextProperty="@nameof(Customer.CompanyName)"
                        LoadData=@OnLoadData IsLoading=@isLoading OpenOnFocus="true"
                        Style="width: 13rem"
                        InputAttributes="@(new Dictionary<string, object>() { { "aria-label", "Company Name" } })">
        <LoadingTemplate>
            <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-2">
                <RadzenProgressBarCircular Size="ProgressBarCircularSize.ExtraSmall" ShowValue="false" Mode="ProgressBarMode.Indeterminate" />
                <RadzenText TextStyle="TextStyle.Body2">Loading customers...</RadzenText>
            </RadzenStack>
        </LoadingTemplate>
        <EmptyTemplate>
            <RadzenText TextStyle="TextStyle.Body2" class="rz-p-2 rz-text-align-center" Style="color: var(--rz-text-tertiary-color);">
                No customers match your search.
            </RadzenText>
        </EmptyTemplate>
    </RadzenAutoComplete>
    <RadzenText TextStyle="TextStyle.Body2" class="rz-pt-2">
        Focus the input to see the empty state, then type e.g. <strong>"France"</strong> to see the loading and result states.
    </RadzenText>
</div>

@code {
    string companyName;
    IEnumerable<Customer> customers;
    bool isLoading;

    async Task OnLoadData(LoadDataArgs args)
    {
        isLoading = true;
        // Simulate a slow data source so the LoadingTemplate is visible.
        await Task.Delay(800);

        customers = dbContext.Customers
            .Where(c => c.CompanyName.Contains(args.Filter))
            .OrderBy(c => c.CompanyName);

        isLoading = false;
    }
}
```


### AutoComplete with a List of Strings

AutoComplete can work directly with a list of strings without the need to define text and value properties.

```razor
@inherits DbContextPage

<div class="rz-p-12 rz-text-align-center">
    <RadzenAutoComplete @bind-Value=@companyName Data=@companyNames Style="width: 13rem" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Company Name" }})" />
    <RadzenText TextStyle="TextStyle.Body2">Start typing e.g. Al</RadzenText>
    @if (!string.IsNullOrEmpty(companyName))
    {
        <RadzenText TextStyle="TextStyle.Body2">Value is: <strong>@companyName</strong></RadzenText>
    }
</div>

@code {
    string companyName;

    IEnumerable<string> companyNames;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        companyNames = dbContext.Customers.Select(c => c.CompanyName);
    }
}
```


### Multiline AutoComplete

Use the `Multiple` property to enable multiline AutoComplete that supports selecting multiple items.

```razor
@inherits DbContextPage

<div class="rz-p-12 rz-text-align-center">
    <RadzenAutoComplete Multiline="true" @bind-Value=@companyName Data=@customers TextProperty="@nameof(Customer.CompanyName)" Style="width: 13rem" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Company Name" }})" />
    <RadzenText TextStyle="TextStyle.Body2">Start typing e.g. France</RadzenText>
    @if (!string.IsNullOrEmpty(companyName))
    {
        <RadzenText TextStyle="TextStyle.Body2">Value is: <strong>@companyName</strong></RadzenText>
    }
</div>

@code {
    string companyName;

    IEnumerable<Customer> customers;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        customers = dbContext.Customers;
    }
}
```


### Open on Focus

Use the `OpenOnFocus` property to automatically open the AutoComplete dropdown when the input receives focus.

```razor
@inherits DbContextPage

<div class="rz-p-12 rz-text-align-center">
    <RadzenAutoComplete @bind-Value=@companyName Data=@customers TextProperty="@nameof(Customer.CompanyName)" OpenOnFocus="true" Style="width: 13rem" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Company Name" }})" />
    <RadzenText TextStyle="TextStyle.Body2">Start typing e.g. France</RadzenText>
    @if (!string.IsNullOrEmpty(companyName))
    {
        <RadzenText TextStyle="TextStyle.Body2">Value is: <strong>@companyName</strong></RadzenText>
    }
</div>

@code {
    string companyName;

    IEnumerable<Customer> customers;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        customers = dbContext.Customers;
    }
}
```


### ComboBox (restrict to list)

Combine `OpenOnFocus` with a `Change` handler that validates the typed value against the data source to make the AutoComplete behave like a combo box: type to filter, but only values that exist in the list are accepted. A dropdown chevron is added with the same `rz-dropdown-trigger` markup the DropDown uses so it matches the surrounding theme.

```razor
@inherits DbContextPage

<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem" class="rz-p-sm-12">
    <RadzenLabel id="AutoCompleteComboBoxLabel" Text="Select company" Component="AutoCompleteComboBox" />
    <div style="position: relative; display: flex; width: 100%; max-width: 400px;">
        <RadzenAutoComplete @bind-Value=@typedText
                            Data=@companyNames
                            OpenOnFocus="true"
                            MinLength="0"
                            FilterDelay="100"
                            FilterOperator="StringFilterOperator.Contains"
                            FilterCaseSensitivity="FilterCaseSensitivity.CaseInsensitive"
                            Change=@OnChange
                            Style="width: 100%;"
                            InputAttributes="@inputAttributes"
                            Name="AutoCompleteComboBox"
                            aria-labelledby="AutoCompleteComboBoxLabel" />
        <div class="rz-dropdown-trigger rz-corner-right" style="pointer-events: none;">
            <span class="notranslate rz-dropdown-trigger-icon rzi rzi-chevron-down"></span>
        </div>
    </div>
</RadzenStack>

<RadzenText TextStyle="TextStyle.Body2" class="rz-mt-4" TextAlign="TextAlign.Center">
    Committed value: <strong>@committedValue</strong>
</RadzenText>

@code {
    // The committed value — only ever set to a string that exists in the data source.
    string committedValue = "Around the Horn";

    // What the user is currently typing. Two-way bound to the AutoComplete.
    string typedText = "Around the Horn";

    IEnumerable<string> companyNames = Array.Empty<string>();

    // Reserve room for the chevron on the right edge of the input.
    readonly IReadOnlyDictionary<string, object> inputAttributes =
        new Dictionary<string, object> { { "style", "padding-right: 2rem;" } };

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        companyNames = dbContext.Customers
            .Select(c => c.CompanyName)
            .Distinct()
            .ToList();
    }

    // Fires when the user picks an item from the popup OR leaves the input (blur/Enter).
    // Restrict-to-list: if the typed text matches an item, commit it; otherwise revert.
    void OnChange(object value)
    {
        var text = value as string;

        var match = companyNames.FirstOrDefault(c =>
            string.Equals(c, text, StringComparison.OrdinalIgnoreCase));

        if (match != null)
        {
            committedValue = match;
            typedText = match; // normalize casing
        }
        else
        {
            // Reject free text — snap back to the last valid value.
            typedText = committedValue;
        }
    }
}
```


### Disabled AutoComplete

Use `Disabled="true"` to disable the AutoComplete and prevent user interaction.

```razor
@inherits DbContextPage

<div class="rz-p-12 rz-text-align-center">
    <RadzenAutoComplete Disabled="true" @bind-Value=@companyName Data=@customers TextProperty="@nameof(Customer.CompanyName)" Style="width: 13rem" InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "Company Name" }})" />
</div>

@code {
    string companyName = "Some value";

    IEnumerable<Customer> customers;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        customers = dbContext.Customers;
    }
}
```


### AutoComplete Sizes

Use the `InputSize` property to set the AutoComplete size. Available sizes are ExtraSmall, Small, Medium (default), and Large.

```razor
<RadzenStack Gap="1rem" class="rz-p-sm-12">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Large" Style="width: 80px;" />
        <RadzenAutoComplete @bind-Value=@value Data=@items Placeholder="Type 'Item'..." InputSize="InputSize.Large" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Medium" Style="width: 80px;" />
        <RadzenAutoComplete @bind-Value=@value Data=@items Placeholder="Type 'Item'..." InputSize="InputSize.Medium" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Small" Style="width: 80px;" />
        <RadzenAutoComplete @bind-Value=@value Data=@items Placeholder="Type 'Item'..." InputSize="InputSize.Small" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" JustifyContent="JustifyContent.Center" Gap="0.5rem">
        <RadzenLabel Text="Extra Small" Style="width: 80px;" />
        <RadzenAutoComplete @bind-Value=@value Data=@items Placeholder="Type 'Item'..." InputSize="InputSize.ExtraSmall" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
</RadzenStack>

@code {
    string value;
    IEnumerable<string> items = new[] { "Item 1", "Item 2", "Item 3" };
}
```
