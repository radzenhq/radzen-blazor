# Label

Associate descriptive text labels with form inputs for better accessibility and usability. Clicking a label focuses its associated input.

Keywords: label, form, input, accessibility, required, validation, formfield, association, aria

> API reference: [RadzenLabel API](https://blazor.radzen.com/api/label.md)

## Examples

## Blazor Label

The Blazor Label associates descriptive text with a form input for better accessibility - clicking the label focuses its input.

### Basic Label with Input

Use the `Text` property to display label text and the `Component` property to associate the label with an input by matching the input's `Name` property.

```razor
<RadzenStack Gap="1rem" class="rz-p-0 rz-p-md-12">
    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenLabel Text="First Name" Component="FirstNameInput" />
        <RadzenTextBox Name="FirstNameInput" @bind-Value=@firstName Placeholder="Enter your first name" />
    </RadzenStack>

    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenLabel Text="Email Address" Component="EmailInput" />
        <RadzenTextBox Name="EmailInput" @bind-Value=@email Placeholder="your.email@example.com" />
    </RadzenStack>

    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenLabel Text="Password" Component="PasswordInput" />
        <RadzenPassword Name="PasswordInput" @bind-Value=@password />
    </RadzenStack>

    <RadzenText TextStyle="TextStyle.Caption" class="rz-mt-2">
        Click on any label to focus its associated input field.
    </RadzenText>
</RadzenStack>

@code {
    string firstName = "";
    string email = "";
    string password = "";
}
```


### Labels with Different Input Types

RadzenLabel works seamlessly with all Radzen input components, creating proper label-input associations for enhanced accessibility.

```razor
<RadzenStack Gap="1.5rem" class="rz-p-0 rz-p-md-12">
    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenLabel Text="Dropdown Selection" Component="DropdownInput" />
        <RadzenDropDown Name="DropdownInput" @bind-Value=@selectedCountry Data=@countries Placeholder="Select a country" Style="width: 100%; max-width: 300px;" />
    </RadzenStack>

    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenLabel Text="Date of Birth" Component="DateInput" />
        <RadzenDatePicker Name="DateInput" @bind-Value=@dateOfBirth Style="width: 100%; max-width: 300px;" />
    </RadzenStack>

    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenLabel Text="Age" Component="NumericInput" />
        <RadzenNumeric Name="NumericInput" @bind-Value=@age Min="0" Max="120" Style="width: 100%; max-width: 300px;" />
    </RadzenStack>

    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenLabel Text="Comments" Component="TextAreaInput" />
        <RadzenTextArea Name="TextAreaInput" @bind-Value=@comments Rows="3" Style="width: 100%; max-width: 300px;" />
    </RadzenStack>

    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenLabel Text="Newsletter Subscription" Component="CheckboxInput" />
        <RadzenCheckBox Name="CheckboxInput" @bind-Value=@subscribed />
    </RadzenStack>
</RadzenStack>

@code {
    string selectedCountry;
    List<string> countries = new List<string> { "USA", "Canada", "UK", "Germany", "France", "Japan" };
    DateTime? dateOfBirth;
    int? age;
    string comments;
    bool subscribed;
}
```


### Label with Custom Content

Use `ChildContent` to create labels with rich content including icons, badges, required indicators, or formatting.

```razor
<RadzenStack Gap="1.5rem" class="rz-p-0 rz-p-md-12">
    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenLabel Component="UsernameInput">
            <RadzenIcon Icon="person" class="rz-mr-1" /> Username
        </RadzenLabel>
        <RadzenTextBox Name="UsernameInput" @bind-Value=@username Style="width: 100%; max-width: 300px;" />
    </RadzenStack>

    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenLabel Component="PriorityInput">
            Priority Level <RadzenBadge BadgeStyle="BadgeStyle.Info" Text="New" IsPill="true" class="rz-ml-2" />
        </RadzenLabel>
        <RadzenDropDown Name="PriorityInput" @bind-Value=@priority Data=@priorities Style="width: 100%; max-width: 300px;" />
    </RadzenStack>

    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenLabel Component="DescriptionInput">
            <strong>Description</strong> <em>(optional)</em>
        </RadzenLabel>
        <RadzenTextArea Name="DescriptionInput" @bind-Value=@description Rows="3" Style="width: 100%; max-width: 300px;" />
    </RadzenStack>

    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenLabel Component="ColorInput">
            Choose Color <RadzenIcon Icon="palette" Style="color: var(--rz-primary);" class="rz-ml-1" />
        </RadzenLabel>
        <RadzenColorPicker Name="ColorInput" @bind-Value=@selectedColor Style="width: 100%; max-width: 300px;" />
    </RadzenStack>
</RadzenStack>

@code {
    string username;
    string priority = "Medium";
    List<string> priorities = new List<string> { "Low", "Medium", "High", "Critical" };
    string description;
    string selectedColor;
}
```


### Required Field Indicators

Add visual indicators for required fields using custom content with asterisks, badges, or other markers.

```razor
<RadzenTemplateForm Data=@model class="rz-p-0 rz-p-md-12">
    <RadzenStack Gap="1.5rem">
        <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
            <RadzenLabel Component="EmailRequiredInput">
                Email <span style="color: var(--rz-danger);">*</span>
            </RadzenLabel>
            <RadzenTextBox Name="EmailRequiredInput" @bind-Value=@model.Email Style="width: 100%; max-width: 400px;" />
            <RadzenRequiredValidator Component="EmailRequiredInput" Text="Email is required" />
        </RadzenStack>

        <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
            <RadzenLabel Component="NameRequiredInput">
                Full Name <RadzenBadge BadgeStyle="BadgeStyle.Danger" Text="Required" Variant="Variant.Outlined" IsPill="true" class="rz-ml-1" />
            </RadzenLabel>
            <RadzenTextBox Name="NameRequiredInput" @bind-Value=@model.FullName Style="width: 100%; max-width: 400px;" />
            <RadzenRequiredValidator Component="NameRequiredInput" Text="Full Name is required" />
        </RadzenStack>

        <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
            <RadzenLabel Component="CountryRequiredInput">
                <RadzenStack Orientation="Orientation.Horizontal" Gap="0.25rem" AlignItems="AlignItems.Center">
                    <RadzenText>Country</RadzenText>
                    <RadzenIcon Icon="star" IconStyle="IconStyle.Danger" Style="font-size: 0.6rem; color: var(--rz-danger);" />
                </RadzenStack>
            </RadzenLabel>
            <RadzenDropDown Name="CountryRequiredInput" @bind-Value=@model.Country Data=@countries Placeholder="Select a country" Style="width: 100%; max-width: 400px;" />
            <RadzenRequiredValidator Component="CountryRequiredInput" Text="Country is required" />
        </RadzenStack>

        <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
            <RadzenLabel Text="Phone (Optional)" Component="PhoneInput" />
            <RadzenTextBox Name="PhoneInput" @bind-Value=@model.Phone Style="width: 100%; max-width: 400px;" />
        </RadzenStack>

        <RadzenButton ButtonType="ButtonType.Submit" Text="Submit Form" Style="width: 100%; max-width: 400px;" />
    </RadzenStack>
</RadzenTemplateForm>

@code {
    class FormModel
    {
        public string Email { get; set; }
        public string FullName { get; set; }
        public string Country { get; set; }
        public string Phone { get; set; }
    }

    FormModel model = new FormModel();
    List<string> countries = new List<string> { "USA", "Canada", "UK", "Germany", "France", "Japan", "Australia" };
}
```


### Label Typography

Use the `TextStyle` property to apply the same typography presets available on `RadzenText` (H1–H6, Subtitle, Body, Caption, Overline, etc.) without losing the semantic `&lt;label&gt;` element or its association with an input.

```razor
<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenCard class="rz-p-4" Variant="Variant.Outlined">
        <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Start" Wrap="FlexWrap.Wrap">
            <RadzenStack Orientation="Orientation.Vertical" Gap="4px">
                TextStyle
                <RadzenSelectBar @bind-Value="@textStyle" TextProperty="Text" ValueProperty="Value"
                                 Data="@textStyleOptions" Size="ButtonSize.Small" class="rz-display-none rz-display-xl-flex"
                                 InputAttributes="@(new Dictionary<string,object>(){ { "aria-label", "text style" }})" />
                <RadzenDropDown @bind-Value="@textStyle" TextProperty="Text" ValueProperty="Value"
                                Data="@textStyleOptions" class="rz-display-inline-flex rz-display-xl-none"
                                aria-label="text style" />
            </RadzenStack>
        </RadzenStack>
    </RadzenCard>
    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenLabel Text="The quick brown fox" TextStyle="@textStyle" Component="LabelTextStyleConfigPreview" />
        <RadzenTextBox Name="LabelTextStyleConfigPreview" @bind-Value=@previewValue Placeholder="Click the label to focus me" />
    </RadzenStack>
</RadzenStack>
@code {
    TextStyle textStyle = TextStyle.H4;
    string previewValue = "";

    static readonly TextStyle[] excluded = new[]
    {
        TextStyle.DisplayH1, TextStyle.DisplayH2, TextStyle.DisplayH3,
        TextStyle.DisplayH4, TextStyle.DisplayH5, TextStyle.DisplayH6,
    };

    IEnumerable<object> textStyleOptions = Enum.GetValues(typeof(TextStyle))
        .Cast<TextStyle>()
        .Where(t => Array.IndexOf(excluded, t) < 0)
        .Select(t => new { Text = t.ToString(), Value = t });
}
```


### Label Styling

Customize label appearance using the `Style` and `class` properties for different font sizes, colors, and weights.

```razor
<RadzenStack Gap="1.5rem" class="rz-p-0 rz-p-md-12">
    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenLabel Text="Default Label" Component="Input1" />
        <RadzenTextBox Name="Input1" @bind-Value=@value1 Style="max-width: 300px;" />
    </RadzenStack>

    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenLabel Text="Bold Label" Component="Input2" Style="font-weight: bold;" />
        <RadzenTextBox Name="Input2" @bind-Value=@value2 Style="max-width: 300px;" />
    </RadzenStack>

    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenLabel Text="Large Label" Component="Input3" Style="font-size: 1.2rem;" />
        <RadzenTextBox Name="Input3" @bind-Value=@value3 Style="max-width: 300px;" />
    </RadzenStack>

    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenLabel Text="Colored Label" Component="Input4" Style="color: var(--rz-primary); font-weight: 500;" />
        <RadzenTextBox Name="Input4" @bind-Value=@value4 Style="max-width: 300px;" />
    </RadzenStack>

    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenLabel Text="Small Uppercase Label" Component="Input5" Style="font-size: 0.75rem; text-transform: uppercase; letter-spacing: 0.5px; color: var(--rz-text-secondary-color);" />
        <RadzenTextBox Name="Input5" @bind-Value=@value5 Style="max-width: 300px;" />
    </RadzenStack>

    <RadzenStack Orientation="Orientation.Vertical" Gap="0.5rem">
        <RadzenLabel Component="Input6" class="rz-text-secondary-color">
            <em>Italic styled label</em>
        </RadzenLabel>
        <RadzenTextBox Name="Input6" @bind-Value=@value6 Style="max-width: 300px;" />
    </RadzenStack>
</RadzenStack>

@code {
    string value1, value2, value3, value4, value5, value6;
}
```
