# RequiredValidator

Demonstration and configuration of the Radzen Blazor Required Validator component.

Keywords: validator, validation, required

> API reference: [RadzenRequiredValidator API](https://blazor.radzen.com/api/requiredvalidator.md)

## Examples

## Blazor RequiredValidator

Enforce mandatory field input with configurable error messages.

```razor
<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenCard Variant="Variant.Outlined" Style="width: 100%">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem">
            <RadzenCheckBox @bind-Value=@popup Name="popup"></RadzenCheckBox>
            <RadzenLabel Text="Display validators as popup" Component="popup" />
        </RadzenStack>
    </RadzenCard>

    <RadzenTemplateForm TItem="Model" Data=@model Submit=@OnSubmit InvalidSubmit=@OnInvalidSubmit>
        <RadzenFieldset Text="Personal information">
            <RadzenStack Gap="2rem" class="rz-p-4 rz-p-md-12">
                <RadzenRow AlignItems="AlignItems.Center" RowGap="0.25rem">
                    <RadzenColumn Size="12" SizeMD="4" class="rz-text-align-start rz-text-align-md-end">
                        <RadzenLabel Text="First Name" Component="FirstName" />
                    </RadzenColumn>
                    <RadzenColumn Size="12" SizeMD="8">
                        <RadzenTextBox Name="FirstName" @bind-Value=@model.FirstName Style="display: block; width: 100%;" />
                        <RadzenRequiredValidator Component="FirstName" Text="First name is required" Popup=@popup Style="position: absolute"/>
                    </RadzenColumn>
                </RadzenRow>
                <RadzenRow AlignItems="AlignItems.Center" RowGap="0.25rem">
                    <RadzenColumn Size="12" SizeMD="4" class="rz-text-align-start rz-text-align-md-end">
                        <RadzenLabel Text="Last Name" Component="LastName" />
                    </RadzenColumn>
                    <RadzenColumn Size="12" SizeMD="8">
                        <RadzenTextBox Name="LastName" @bind-Value=@model.LastName Style="display: block; width: 100%;" />
                        <RadzenRequiredValidator Component="LastName" Text="Last name is required" Popup=@popup Style="position: absolute"/>
                    </RadzenColumn>
                </RadzenRow>
                <RadzenRow AlignItems="AlignItems.Center" class="rz-mt-4">
                    <RadzenColumn Size="12" Offset="0" SizeMD="8" OffsetMD="4">
                        <RadzenButton ButtonType="ButtonType.Submit" Text="Submit"></RadzenButton>
                    </RadzenColumn>
                </RadzenRow>
            </RadzenStack>
        </RadzenFieldset>
    </RadzenTemplateForm>

    <EventConsole @ref=@console />
</RadzenStack>

@code {
    class Model
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    bool popup;

    Model model = new Model();
    EventConsole console;

    void OnSubmit(Model model)
    {
        console.Log($"Submit: {JsonSerializer.Serialize(model, new JsonSerializerOptions() {  WriteIndented = true })}");
    }

    void OnInvalidSubmit(FormInvalidSubmitEventArgs args)
    {
        console.Log($"InvalidSubmit: {JsonSerializer.Serialize(args, new JsonSerializerOptions() {  WriteIndented = true })}");
    }
}
```


### Validate RadzenDropDown

Use `RequiredValidator` to ensure a dropdown selection is made before form submission.

```razor
@inherits DbContextPage

<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenTemplateForm TItem="Model" Data=@model Submit="@OnSubmit" InvalidSubmit="@OnIvalidSubmit">
        <RadzenFieldset Text="Category and products">
            <RadzenStack Gap="2rem" class="rz-p-4 rz-p-md-12">
                <RadzenRow AlignItems="AlignItems.Center" RowGap="0.25rem">
                    <RadzenColumn Size="12" SizeMD="4" class="rz-text-align-start rz-text-align-md-end">
                        <RadzenLabel Text="Category" />
                    </RadzenColumn>
                    <RadzenColumn Size="12" SizeMD="8">
                        <RadzenDropDown Name="Category" Change="@CategoryChange" @bind-Value=@model.CategoryName Data="@categories" TextProperty="@nameof(Category.CategoryName)" ValueProperty="@nameof(Category.CategoryName)" Placeholder="Select category" Style="display: block; width: 100%;" />
                        <RadzenRequiredValidator Component="Category" Text="Category is required" Style="position: absolute"/>
                    </RadzenColumn>
                </RadzenRow>
                <RadzenRow AlignItems="AlignItems.Center" RowGap="0.25rem">
                    <RadzenColumn Size="12" SizeMD="4" class="rz-text-align-start rz-text-align-md-end">
                        <RadzenLabel Text="Last Name" Component="LastName" />
                    </RadzenColumn>
                    <RadzenColumn Size="12" SizeMD="8">
                        <RadzenDropDown MaxSelectedLabels="2" Multiple="true" Name="Products" @bind-Value=@model.Products Data="@products" TextProperty="@nameof(Product.ProductName)" ValueProperty="@nameof(Product.ProductName)" Placeholder="Select products" Style="display: block; width: 100%;" />
                        <RadzenRequiredValidator Component="Products" Text="Products are required" Style="position: absolute"/>
                    </RadzenColumn>
                </RadzenRow>
                <RadzenRow AlignItems="AlignItems.Center" class="rz-mt-4">
                    <RadzenColumn Size="12" Offset="0" SizeMD="8" OffsetMD="4">
                        <RadzenButton ButtonType="ButtonType.Submit" Text="Submit"></RadzenButton>
                    </RadzenColumn>
                </RadzenRow>
            </RadzenStack>
        </RadzenFieldset>
    </RadzenTemplateForm>
</RadzenStack>

@code {
    class Model
    {
        public string CategoryName { get; set; }
        public IEnumerable<string> Products { get; set; }
    }

    Model model = new Model();
    IEnumerable<Category> categories;
    IEnumerable<Product> products;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        categories = dbContext.Categories.ToList();
    }

    void CategoryChange()
    {
        products = dbContext.Products.Where(p => p.Category.CategoryName == model.CategoryName).ToList();
        model.Products = null;
    }

    void OnSubmit()
    {
        NotificationService.Notify(NotificationSeverity.Success, "Success", "Form submitted successfully!");
    }

    void OnIvalidSubmit()
    {
        NotificationService.Notify(NotificationSeverity.Error, "Error", "Form has errors!");
    }
}
```
