# Localization

How to localize Radzen Blazor Components using resource files, satellite assemblies, or the ILocalizer interface.

Keywords: localization, globalization, culture, translation, language, i18n, l10n, resource, resx, satellite

## Examples

## Localization

Radzen Blazor Components support resource-based localization out of the box. All user-facing text (labels, tooltips, ARIA labels, filter operators, paging text, etc.) can be translated using .NET's standard localization infrastructure.

### Live demo

Pick a language to translate the components below. Radzen Blazor ships with built-in translations for German, French, Spanish, Italian and Japanese. Open the filter menu, the column picker, the group panel and the paging summary on the grid, and the date picker, to see the localized strings update instantly.

```razor
<RadzenStack Gap="1rem">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem" Wrap="FlexWrap.Wrap">
        <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.P" class="rz-mb-0">Language</RadzenText>
        <RadzenSelectBar @bind-Value="@culture" Data="@cultures" TextProperty="Name" ValueProperty="Culture" Size="ButtonSize.Small" />
    </RadzenStack>

    <CascadingValue Name="DefaultUICulture" Value="@culture">
    <CascadingValue Name="DefaultCulture" Value="@culture">
        <RadzenDataGrid @bind-Value="@selectedProducts" Data="@products" TItem="Product" PageSize="5"
                        AllowSorting="true" AllowFiltering="true" AllowColumnPicker="true"
                        AllowPaging="true" AllowGrouping="true"
                        FilterMode="FilterMode.Advanced" SelectionMode="DataGridSelectionMode.Single">
            <Columns>
                <RadzenDataGridColumn Property="@nameof(Product.Name)" Title="Name" />
                <RadzenDataGridColumn Property="@nameof(Product.Category)" Title="Category" />
                <RadzenDataGridColumn Property="@nameof(Product.Quantity)" Title="Quantity" TextAlign="TextAlign.Right" />
                <RadzenDataGridColumn Property="@nameof(Product.InStock)" Title="In stock" />
            </Columns>
        </RadzenDataGrid>

        <RadzenStack Orientation="Orientation.Horizontal" Gap="2rem" Wrap="FlexWrap.Wrap" AlignItems="AlignItems.Start">
            <RadzenStack Gap="0.5rem">
                <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.P" class="rz-mb-0">DatePicker</RadzenText>
                <RadzenDatePicker @bind-Value="@date" Inline="true" ShowTime="true" />
            </RadzenStack>
            <RadzenStack Gap="0.5rem">
                <RadzenText TextStyle="TextStyle.Subtitle2" TagName="TagName.P" class="rz-mb-0">Login</RadzenText>
                <RadzenTemplateForm Data=@("Localization")>
                    <RadzenLogin AllowRegister="true" AllowResetPassword="true" AllowRememberMe="true" />
                </RadzenTemplateForm>
            </RadzenStack>
        </RadzenStack>
    </CascadingValue>
    </CascadingValue>
</RadzenStack>

@code {
    class Product
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public bool InStock { get; set; }
    }

    class Language
    {
        public string Name { get; set; } = string.Empty;
        public CultureInfo Culture { get; set; } = CultureInfo.InvariantCulture;
    }

    readonly Language[] cultures =
    [
        new() { Name = "English", Culture = new CultureInfo("en") },
        new() { Name = "Deutsch", Culture = new CultureInfo("de") },
        new() { Name = "Français", Culture = new CultureInfo("fr") },
        new() { Name = "Español", Culture = new CultureInfo("es") },
        new() { Name = "Italiano", Culture = new CultureInfo("it") },
        new() { Name = "日本語", Culture = new CultureInfo("ja") },
    ];

    CultureInfo culture = new("de");
    DateTime date = new(2025, 6, 4, 10, 30, 0);
    IList<Product> selectedProducts = [];

    readonly List<Product> products =
    [
        new() { Name = "Espresso machine", Category = "Appliances", Quantity = 12, InStock = true },
        new() { Name = "Office chair", Category = "Furniture", Quantity = 0, InStock = false },
        new() { Name = "Wireless mouse", Category = "Electronics", Quantity = 134, InStock = true },
        new() { Name = "Notebook", Category = "Stationery", Quantity = 540, InStock = true },
        new() { Name = "Standing desk", Category = "Furniture", Quantity = 7, InStock = true },
        new() { Name = "Mechanical keyboard", Category = "Electronics", Quantity = 0, InStock = false },
        new() { Name = "Desk lamp", Category = "Appliances", Quantity = 56, InStock = true },
        new() { Name = "Ballpoint pen", Category = "Stationery", Quantity = 980, InStock = true },
    ];
}
```


### How it works

Every localizable string property in Radzen components falls back to a resource key in `RadzenStrings.resx` when not explicitly set. The lookup goes through the `Localizer` class which checks for a custom `ILocalizer` implementation first, then falls back to the embedded resource file. This means:

### Option 1: ILocalizer (recommended)

Implement the `ILocalizer` interface and register it in the DI container to provide translations for all components.
1. Create a class that implements `ILocalizer`:
2. Register it in `Program.cs` before `AddRadzenComponents()`:

### Option 2: Satellite assemblies

You can provide culture-specific `.resx` files that .NET compiles into satellite assemblies. This is the standard .NET localization mechanism.
1. Create a resource file named `RadzenStrings.[culture].resx` (e.g. `RadzenStrings.de.resx`) in your project.
2. Add the translated values using the same resource keys (e.g. `Pager_FirstPageTitle`, `DataGrid_FilterText`). You only need to include the keys you want to translate.
3. Configure the resource file in your `.csproj` to use the `Radzen.Blazor.RadzenStrings` resource name:
The `ResourceManager` will automatically load the correct culture at runtime based on `CultureInfo.CurrentUICulture`.

### Option 3: Parameter override (per-instance)

You can still set localized text directly on individual component instances. This takes the highest priority and overrides both resource files and `ILocalizer`.
This example shows a German login form using direct parameter values:

```razor
<RadzenCard class="rz-my-12 rz-mx-auto rz-p-4 rz-p-md-12" style="max-width: 600px;">
    
    <RadzenTemplateForm Data=@("Localization")>
        <RadzenLogin AllowRegister="true" AllowResetPassword="true"
                        AllowRememberMe="true"
                        LoginText="Einloggen" UserText="Benutzername" PasswordText="Passwort"
                        UserRequired="Benutzername erforderlich"
                        PasswordRequired="Passwort erforderlich"
                        RegisterText="Registrieren"
                        RegisterMessageText="Sie haben noch keinen Account?"
                        ResetPasswordText="Passwort zurücksetzen"
                        RememberMeText="Behalte mich in Erinnerung" />
    </RadzenTemplateForm>
    
</RadzenCard>
```


### Resource keys

Resource keys follow the convention `ComponentName_PropertyName`. Use `nameof(RadzenStrings.Key)` for compile-time safety. Below are some common examples:

#### DataGrid


#### Pager


#### Other components

All localizable resource keys are defined in `RadzenStrings`. Use your IDE's autocomplete with `nameof(RadzenStrings.)` to discover all available keys. The naming convention is `ComponentName_PropertyName`, for example:

### Culture resolution

Each Radzen component resolves the UI culture in this order:
To set the culture for all components in your app, configure `CultureInfo.DefaultThreadCurrentUICulture` in `Program.cs`:

### Priority order

When a component renders a localizable string, the value is resolved in this order (highest priority first):
