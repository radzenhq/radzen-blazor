# TemplateForm

The Blazor Form (TemplateForm) builds data-bound forms with built-in validation and submit handling.

Keywords: form, edit, validation, submit, editcontext

> API reference: [RadzenTemplateForm API](https://blazor.radzen.com/api/templateform.md)

## Examples

## Blazor TemplateForm

The Blazor Form (TemplateForm) builds data-bound forms with built-in validation and a custom EditContext.

### Basic Usage

Bind a model to the form via the `Data` property, add Radzen validators such as `RadzenRequiredValidator` and `RadzenEmailValidator`, and handle the `Submit` and `InvalidSubmit` events.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenTemplateForm TItem="Registration" Data=@model Submit=@OnSubmit InvalidSubmit=@OnInvalidSubmit>
        <RadzenStack Gap="1rem">
            <RadzenRow AlignItems="AlignItems.Center" RowGap="0.25rem">
                <RadzenColumn Size="12" SizeMD="4" class="rz-text-align-start rz-text-align-md-end">
                    <RadzenLabel Text="First Name" Component="FirstName" />
                </RadzenColumn>
                <RadzenColumn Size="12" SizeMD="8">
                    <RadzenTextBox Name="FirstName" @bind-Value=@model.FirstName Style="display: block; width: 100%;" />
                    <RadzenRequiredValidator Component="FirstName" Text="First name is required." Style="position: absolute" />
                </RadzenColumn>
            </RadzenRow>
            <RadzenRow AlignItems="AlignItems.Center" RowGap="0.25rem">
                <RadzenColumn Size="12" SizeMD="4" class="rz-text-align-start rz-text-align-md-end">
                    <RadzenLabel Text="Last Name" Component="LastName" />
                </RadzenColumn>
                <RadzenColumn Size="12" SizeMD="8">
                    <RadzenTextBox Name="LastName" @bind-Value=@model.LastName Style="display: block; width: 100%;" />
                    <RadzenRequiredValidator Component="LastName" Text="Last name is required." Style="position: absolute" />
                </RadzenColumn>
            </RadzenRow>
            <RadzenRow AlignItems="AlignItems.Center" RowGap="0.25rem">
                <RadzenColumn Size="12" SizeMD="4" class="rz-text-align-start rz-text-align-md-end">
                    <RadzenLabel Text="Email" Component="Email" />
                </RadzenColumn>
                <RadzenColumn Size="12" SizeMD="8">
                    <RadzenTextBox Name="Email" @bind-Value=@model.Email Style="display: block; width: 100%;" />
                    <RadzenRequiredValidator Component="Email" Text="Email is required." Style="position: absolute" />
                    <RadzenEmailValidator Component="Email" Text="Provide a valid email address." Style="position: absolute" />
                </RadzenColumn>
            </RadzenRow>
            <RadzenRow AlignItems="AlignItems.Center" class="rz-mt-4">
                <RadzenColumn Size="12" Offset="0" SizeMD="8" OffsetMD="4">
                    <RadzenButton ButtonType="ButtonType.Submit" Text="Submit" />
                </RadzenColumn>
            </RadzenRow>
        </RadzenStack>
    </RadzenTemplateForm>
    <EventConsole @ref=@console />
</div>

@code {
    class Registration
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }

    Registration model = new Registration();
    EventConsole console;

    void OnSubmit(Registration args)
    {
        console.Log($"Submit: {JsonSerializer.Serialize(args, new JsonSerializerOptions() { WriteIndented = true })}");
    }

    void OnInvalidSubmit(FormInvalidSubmitEventArgs args)
    {
        console.Log($"InvalidSubmit: {JsonSerializer.Serialize(args, new JsonSerializerOptions() { WriteIndented = true })}");
    }
}
```


### Custom EditContext

Provide your own `EditContext` instead of `Data` for advanced scenarios such as programmatic validation via the `EditContext.Validate()` method.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenTemplateForm TItem="ContactInfo" EditContext=@editContext Submit=@OnSubmit InvalidSubmit=@OnInvalidSubmit>
        <DataAnnotationsValidator />
        <RadzenStack Gap="1rem">
            <RadzenRow AlignItems="AlignItems.Center" RowGap="0.25rem">
                <RadzenColumn Size="12" SizeMD="4" class="rz-text-align-start rz-text-align-md-end">
                    <RadzenLabel Text="Name" Component="Name" />
                </RadzenColumn>
                <RadzenColumn Size="12" SizeMD="8">
                    <RadzenTextBox Name="Name" @bind-Value=@model.Name Style="display: block; width: 100%;" />
                    <RadzenDataAnnotationValidator Component="Name" Style="position: absolute" />
                </RadzenColumn>
            </RadzenRow>
            <RadzenRow AlignItems="AlignItems.Center" RowGap="0.25rem">
                <RadzenColumn Size="12" SizeMD="4" class="rz-text-align-start rz-text-align-md-end">
                    <RadzenLabel Text="Phone" Component="Phone" />
                </RadzenColumn>
                <RadzenColumn Size="12" SizeMD="8">
                    <RadzenTextBox Name="Phone" @bind-Value=@model.Phone Style="display: block; width: 100%;" />
                    <RadzenDataAnnotationValidator Component="Phone" Style="position: absolute" />
                </RadzenColumn>
            </RadzenRow>
            <RadzenRow AlignItems="AlignItems.Center" class="rz-mt-4">
                <RadzenColumn Size="12" Offset="0" SizeMD="8" OffsetMD="4">
                    <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem">
                        <RadzenButton ButtonType="ButtonType.Submit" Text="Submit" />
                        <RadzenButton Text="Validate" Click=@Validate ButtonStyle="ButtonStyle.Light" Variant="Variant.Flat" />
                    </RadzenStack>
                </RadzenColumn>
            </RadzenRow>
        </RadzenStack>
    </RadzenTemplateForm>
    <EventConsole @ref=@console />
</div>

@code {
    class ContactInfo
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Phone is required.")]
        [Phone(ErrorMessage = "Provide a valid phone number.")]
        public string Phone { get; set; }
    }

    ContactInfo model = new ContactInfo();
    EditContext editContext;
    EventConsole console;

    protected override void OnInitialized()
    {
        editContext = new EditContext(model);
    }

    void OnSubmit(ContactInfo args)
    {
        console.Log($"Submit: {JsonSerializer.Serialize(args, new JsonSerializerOptions() { WriteIndented = true })}");
    }

    void OnInvalidSubmit(FormInvalidSubmitEventArgs args)
    {
        console.Log($"InvalidSubmit: {JsonSerializer.Serialize(args, new JsonSerializerOptions() { WriteIndented = true })}");
    }

    void Validate()
    {
        var isValid = editContext.Validate();
        console.Log($"Validate: IsValid = {isValid}");
    }
}
```


### Form Action

Set the `Action` and `Method` properties to perform a traditional HTML form submission to a URL.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenTemplateForm TItem="SearchQuery" Data=@model Action="https://www.google.com/search" Method="get" target="_blank">
        <RadzenStack Gap="1rem">
            <RadzenRow AlignItems="AlignItems.Center" RowGap="0.25rem">
                <RadzenColumn Size="12" SizeMD="4" class="rz-text-align-start rz-text-align-md-end">
                    <RadzenLabel Text="Search" Component="q" />
                </RadzenColumn>
                <RadzenColumn Size="12" SizeMD="8">
                    <RadzenTextBox Name="q" @bind-Value=@model.Query Style="display: block; width: 100%;" />
                    <RadzenRequiredValidator Component="q" Text="Search query is required." Style="position: absolute" />
                </RadzenColumn>
            </RadzenRow>
            <RadzenRow AlignItems="AlignItems.Center" class="rz-mt-4">
                <RadzenColumn Size="12" Offset="0" SizeMD="8" OffsetMD="4">
                    <RadzenButton ButtonType="ButtonType.Submit" Text="Search" />
                </RadzenColumn>
            </RadzenRow>
        </RadzenStack>
    </RadzenTemplateForm>
</div>

@code {
    class SearchQuery
    {
        public string Query { get; set; }
    }

    SearchQuery model = new SearchQuery();
}
```
