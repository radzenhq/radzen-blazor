# FormField

The Blazor FormField wraps an input with a floating label, helper text, and validation styling.

Keywords: form, label, floating, float, edit, outline, input, helper, valid

> API reference: [RadzenFormField API](https://blazor.radzen.com/api/formfield.md)

## Examples

## Blazor FormField

The Blazor FormField wraps an input with a floating label, helper text, and validation messages, with multiple input variants.

### Variants

The FormField can be easily customized to fit a wide range of form input needs. Set the `Variant` property to use Outlined, Flat, Filled, or Text variants.

```razor
<RadzenRow AlignItems="AlignItems.End" Wrap="FlexWrap.Wrap" Gap="1rem" class="rz-p-sm-12">
    <RadzenColumn Size="12" SizeMD="6" SizeLG="3">
        <RadzenFormField Text="Outlined/Default" Component="FormFieldTextBoxOutlined" Style="width: 100%;">
            <RadzenTextBox id="FormFieldTextBoxOutlined" @bind-Value="@value" />
        </RadzenFormField>
    </RadzenColumn>
    <RadzenColumn Size="12" SizeMD="6" SizeLG="3">
        <RadzenFormField Text="Text" Variant="Variant.Text" Component="FormFieldTextBoxText" Style="width: 100%;">
            <RadzenTextBox id="FormFieldTextBoxText" @bind-Value="@value" />
        </RadzenFormField>
    </RadzenColumn>
    <RadzenColumn Size="12" SizeMD="6" SizeLG="3">
        <RadzenFormField Text="Flat" Variant="Variant.Flat" Component="FormFieldTextBoxFlat" Style="width: 100%;">
            <RadzenTextBox id="FormFieldTextBoxFlat" @bind-Value="@value" />
        </RadzenFormField>
    </RadzenColumn>
    <RadzenColumn Size="12" SizeMD="6" SizeLG="3">
        <RadzenFormField Text="Filled" Variant="Variant.Filled" Component="FormFieldTextBoxFilled" Style="width: 100%;">
            <RadzenTextBox id="FormFieldTextBoxFilled" @bind-Value="@value" />
        </RadzenFormField>
    </RadzenColumn>
</RadzenRow>

@code {
    string value = "";
}
```


### Input types

The FormField can be used to render different types of form input components, such as RadzenTextBox, RadzenPassword, RadzenDropDown and more.

```razor
@inherits DbContextPage

<div class="rz-p-0 rz-p-md-12">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-4 rz-mb-6 rz-border-radius-1" Style="border: var(--rz-grid-cell-border);">
        <RadzenLabel Text="Variant:" />
        <RadzenSelectBar @bind-Value="@variant" TextProperty="Text" ValueProperty="Value" Data="@(Enum.GetValues(typeof(Variant)).Cast<Variant>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" class="rz-display-none rz-display-xl-flex" />
        <RadzenDropDown @bind-Value="@variant" TextProperty="Text" ValueProperty="Value" Data="@(Enum.GetValues(typeof(Variant)).Cast<Variant>().Select(t => new { Text = $"{t}", Value = t }))" class="rz-display-inline-flex rz-display-xl-none" />
    </RadzenStack>
    <RadzenRow Gap="1rem">
        <RadzenColumn Size="12" SizeSM="6">
            <RadzenStack>
                <RadzenFormField Text="RadzenTextBox" Variant="@variant">
                    <RadzenTextBox @bind-Value="@value" />
                </RadzenFormField>
                <RadzenFormField Text="RadzenNumeric" Variant="@variant">
                    <RadzenNumeric @bind-Value="@intValue" />
                </RadzenFormField>
                <RadzenFormField Text="RadzenPassword" Variant="@variant">
                    <RadzenPassword @bind-Value="@value" />
                </RadzenFormField>
                <RadzenFormField Text="RadzenDropDown" Variant="@variant">
                    <RadzenDropDown Data=@companyNames @bind-Value="@dropDownValue" />
                </RadzenFormField>
                <RadzenFormField Text="RadzenAutoComplete" Variant="@variant">
                    <RadzenAutoComplete Data=@companyNames @bind-Value="@autoCompleteValue" />
                </RadzenFormField>
                <RadzenFormField Text="RadzenDropDownDataGrid" Variant="@variant">
                    <RadzenDropDownDataGrid Data=@companyNames @bind-Value="@dropDownDataGridValue" />
                </RadzenFormField>
            </RadzenStack>
        </RadzenColumn>
        <RadzenColumn Size="12" SizeSM="6">
            <RadzenStack>
                <RadzenFormField Text="RadzenDatePicker" Variant="@variant">
                    <RadzenDatePicker @bind-Value="@date" ShowTime="@true" />
                </RadzenFormField>
                <RadzenFormField Text="RadzenTimeSpanPicker" Variant="@variant">
                    <RadzenTimeSpanPicker @bind-Value="@timeSpan" />
                </RadzenFormField>
                <RadzenFormField Text="RadzenColorPicker" Variant="@variant">
                    <RadzenColorPicker @bind-Value="@color" />
                </RadzenFormField>
                <RadzenFormField Text="RadzenTextArea" Variant="@variant">
                    <RadzenTextArea @bind-Value="@value" Rows="4" />
                </RadzenFormField>
                <RadzenFormField Text="RadzenRadioButtonList" Variant="@variant">
                    <RadzenRadioButtonList @bind-Value=@radioButtonValue TValue="int" class="rz-m-4 rz-mt-8">
                        <Items>
                            <RadzenRadioButtonListItem Text="Orders" Value="1" />
                            <RadzenRadioButtonListItem Text="Employees" Value="2" />
                            <RadzenRadioButtonListItem Text="Customers" Value="3" />
                        </Items>
                    </RadzenRadioButtonList>
                </RadzenFormField>
            </RadzenStack>
        </RadzenColumn>
    </RadzenRow>
</div>

@code {
    Variant variant = Variant.Outlined;

    string value = "Text";
    int intValue = 123;
    int radioButtonValue = 1;
    string dropDownValue = "Around the Horn";
    string dropDownDataGridValue = "";
    string autoCompleteValue = "";
    string color = "rgb(68, 58, 110)";
    DateTime? date = DateTime.Today;
    TimeSpan? timeSpan = new TimeSpan(5, 15, 30);

    IEnumerable<string> companyNames;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        companyNames = dbContext.Customers.Select(c => c.CompanyName).Distinct();
    }
}
```


### Start, End, and ChildContent

To render content before or after the input in a RadzenFormField, you need to add `&lt;Start&gt;` or `&lt;End&gt;` elements together with a `&lt;ChildContent&gt;` that contains the input component.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-4 rz-mb-6 rz-border-radius-1" Style="border: var(--rz-grid-cell-border);">
        <RadzenLabel Text="Variant:" />
        <RadzenSelectBar @bind-Value="@variant" TextProperty="Text" ValueProperty="Value" Data="@(Enum.GetValues(typeof(Variant)).Cast<Variant>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" class="rz-display-none rz-display-xl-flex" />
        <RadzenDropDown @bind-Value="@variant" TextProperty="Text" ValueProperty="Value" Data="@(Enum.GetValues(typeof(Variant)).Cast<Variant>().Select(t => new { Text = $"{t}", Value = t }))" class="rz-display-inline-flex rz-display-xl-none" />
    </RadzenStack>

    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.End" JustifyContent="JustifyContent.Center" Wrap="FlexWrap.Wrap" Gap="1rem" class="rz-p-sm-12">
        <RadzenFormField Text="Account" Variant="@variant">
            <Start>
                <RadzenIcon Icon="account_circle" />
            </Start>
            <ChildContent>
                <RadzenTextBox @bind-Value="@value" />
            </ChildContent>
            <End>
                <RadzenIcon Icon="add_circle" IconStyle="IconStyle.Secondary" />
            </End>
        </RadzenFormField>
        <RadzenFormField Text="Credit Card Number" Variant="@variant">
            <Start>
                <RadzenIcon Icon="credit_card" />
            </Start>
            <ChildContent>
                <RadzenMask Mask="**** **** **** ****" CharacterPattern="[0-9]" Placeholder="0000 0000 0000 0000" Name="CardNr" />
            </ChildContent>
        </RadzenFormField>
        <RadzenFormField Text="Password" Variant="@variant">
            <ChildContent>
                <RadzenTextBox @bind-Value="@passwordValue" Visible="@(!password)" />
                <RadzenPassword @bind-Value="@passwordValue" Visible="@password" />
            </ChildContent>
            <End>
                <RadzenButton Icon="@(password ? "visibility" : "visibility_off")" Click="TogglePassword" Variant="Variant.Text" Size="ButtonSize.Small" />
            </End>
        </RadzenFormField>
    </RadzenStack>
</div>

@code {
    string value = "";
    string passwordValue = "password";
    bool password = true;
    Variant variant = Variant.Outlined;

    void TogglePassword()
    {
        password = !password;
    }
}
```


### Floating Label

By default, RadzenFormField has the floating label effect enabled. To disable it and always display the label fixed on top, use `AllowFloatingLabel="false"`.

```razor
<RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.End" JustifyContent="JustifyContent.Center" Wrap="FlexWrap.Wrap" Gap="1rem" class="rz-p-sm-12">
    <RadzenFormField Text="Floating label" >
        <RadzenTextBox @bind-Value="@value" />
    </RadzenFormField>
    <RadzenFormField AllowFloatingLabel="false" Text="Fixed label" >
        <RadzenTextBox @bind-Value="@value" />
    </RadzenFormField>
</RadzenStack>

@code {
    string value = "";
}
```


### Helper text

To display assistive content in a RadzenFormField, add `&lt;Helper&gt;` element after the `&lt;ChildContent&gt;`.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-4 rz-mb-6 rz-border-radius-1" Style="border: var(--rz-grid-cell-border);">
        <RadzenLabel Text="Variant:" />
        <RadzenSelectBar @bind-Value="@variant" TextProperty="Text" ValueProperty="Value" Data="@(Enum.GetValues(typeof(Variant)).Cast<Variant>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" class="rz-display-none rz-display-xl-flex" />
        <RadzenDropDown @bind-Value="@variant" TextProperty="Text" ValueProperty="Value" Data="@(Enum.GetValues(typeof(Variant)).Cast<Variant>().Select(t => new { Text = $"{t}", Value = t }))" class="rz-display-inline-flex rz-display-xl-none" />
    </RadzenStack>

    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.End" JustifyContent="JustifyContent.Center" Wrap="FlexWrap.Wrap" Gap="1rem" class="rz-p-sm-12">
        <RadzenFormField Text="Card Number" Variant="@variant">
            <Start>
                <RadzenIcon Icon="credit_card" />
            </Start>
            <ChildContent>
                <RadzenMask Mask="**** **** **** ****" CharacterPattern="[0-9]" Placeholder="0000 0000 0000 0000" Name="CardNr" />
            </ChildContent>
            <Helper>
                <RadzenText TextStyle="TextStyle.Caption">* required</RadzenText>
            </Helper>
        </RadzenFormField>
    </RadzenStack>
</div>

@code {
    Variant variant = Variant.Outlined;
}
```


### Validation

You can use validators inside a FormField.

```razor
<div class="rz-p-0 rz-p-md-12">
    <RadzenStack Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center" Gap="0.5rem" class="rz-p-4 rz-mb-6 rz-border-radius-1" Style="border: var(--rz-grid-cell-border);">
        <RadzenLabel Text="Variant:" />
        <RadzenSelectBar @bind-Value="@variant" TextProperty="Text" ValueProperty="Value" Data="@(Enum.GetValues(typeof(Variant)).Cast<Variant>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" class="rz-display-none rz-display-xl-flex" />
        <RadzenDropDown @bind-Value="@variant" TextProperty="Text" ValueProperty="Value" Data="@(Enum.GetValues(typeof(Variant)).Cast<Variant>().Select(t => new { Text = $"{t}", Value = t }))" class="rz-display-inline-flex rz-display-xl-none" />
    </RadzenStack>
    <RadzenTemplateForm TItem="Model" Data=@model Submit=@OnSubmit InvalidSubmit=@OnInvalidSubmit>
        <RadzenStack Gap="1rem" class="rz-p-sm-12">
            <RadzenFormField Text="First Name" Variant="@variant">
                <ChildContent>
                    <RadzenTextBox Name="FirstName" @bind-Value=@model.FirstName />
                </ChildContent>
                <Helper>
                    <RadzenRequiredValidator Component="FirstName" Text="First name is required." />
                </Helper>
            </RadzenFormField>
            <RadzenFormField Text="Last Name" Variant="@variant">
                <ChildContent>
                    <RadzenTextBox Name="LastName" @bind-Value=@model.LastName />
                </ChildContent>
                <Helper>
                    <RadzenRequiredValidator Component="LastName" Text="Last name is required." />
                </Helper>
            </RadzenFormField>
            <RadzenButton ButtonType="ButtonType.Submit" Text="Submit" ></RadzenButton>
        </RadzenStack>
    </RadzenTemplateForm>
    <EventConsole @ref=@console />
</div>

@code {
    Variant variant = Variant.Outlined;

    class Model
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

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


### Disabled FormField

To disable a FormField, just set the `Disabled` property of the input component to `true`.

```razor
@inherits DbContextPage

<div class="rz-p-0 rz-p-md-12">
    <RadzenStack Orientation="Orientation.Vertical" Gap="1rem" class="rz-p-4 rz-mb-6 rz-border-radius-1" Style="border: var(--rz-grid-cell-border);">
        <RadzenStack Gap="0.5rem" Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center">
            <RadzenLabel Text="Disable form fields:" />
            <RadzenSwitch @bind-Value="disabled" />
        </RadzenStack>
        <RadzenStack Gap="0.5rem" Orientation="Orientation.Horizontal" AlignItems="AlignItems.Center">
            <RadzenLabel Text="Variant:" />
            <RadzenSelectBar @bind-Value="@variant" TextProperty="Text" ValueProperty="Value" Data="@(Enum.GetValues(typeof(Variant)).Cast<Variant>().Select(t => new { Text = $"{t}", Value = t }))" Size="ButtonSize.Small" class="rz-display-none rz-display-xl-flex" />
            <RadzenDropDown @bind-Value="@variant" TextProperty="Text" ValueProperty="Value" Data="@(Enum.GetValues(typeof(Variant)).Cast<Variant>().Select(t => new { Text = $"{t}", Value = t }))" class="rz-display-inline-flex rz-display-xl-none" />
        </RadzenStack>
    </RadzenStack>

    <RadzenStack Orientation="Orientation.Horizontal" Wrap="FlexWrap.Wrap" Gap="1rem">
        <RadzenFormField Text="RadzenTextBox" Variant="@variant" Style="flex: 1;">
            <RadzenTextBox @bind-Value="@value" Disabled="@disabled" />
        </RadzenFormField>
        <RadzenFormField Text="RadzenNumeric" Variant="@variant" Style="flex: 1;">
            <RadzenNumeric @bind-Value="@intValue" Disabled="@disabled" />
        </RadzenFormField>
        <RadzenFormField Text="RadzenPassword" Variant="@variant" Style="flex: 1;">
            <RadzenPassword @bind-Value="@value" Disabled="@disabled" />
        </RadzenFormField>
        <RadzenFormField Text="RadzenDropDown" Variant="@variant" Style="flex: 1;">
            <RadzenDropDown Data=@companyNames @bind-Value="@dropDownValue"  Disabled="@disabled" />
        </RadzenFormField>
    </RadzenStack>
</div>

@code {
    bool disabled = true;
    Variant variant = Variant.Outlined;

    string value = "Text";
    int intValue = 123;
    string dropDownValue = "Around the Horn";

    IEnumerable<string> companyNames;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        companyNames = dbContext.Customers.Select(c => c.CompanyName).Distinct();
    }
}
```
