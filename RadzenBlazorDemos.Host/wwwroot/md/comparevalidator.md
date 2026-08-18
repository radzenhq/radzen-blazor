# CompareValidator

The Blazor RadzenCompareValidator compares the user input against a predefined value or another component.

Keywords: validator, validation, required, compare

> API reference: [RadzenCompareValidator API](https://blazor.radzen.com/api/comparevalidator.md)

## Examples

## Blazor CompareValidator

Compares user input against a predefined value or another component.

### Basic Usage

RadzenCompareValidator compares the user input against a predefined value or another component.
To use it perform these steps:
Add an input component and set its `Name`. Data-bind its value to a model property via `@-Value=@`. Add RadzenCompareValidator and set its `Component` property to the `Name` of the input component. Set its `Value` property to the value you want to compare with (usually another model property).
Important! RadzenCompareValidator works only inside [RadzenTemplateForm](/templateform).
Here is a typical user registration form which checks if the user entered the same password.

```razor
<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center" class="rz-p-4 rz-border-radius-1" Style="border: var(--rz-grid-cell-border);">
        <RadzenCheckBox @bind-Value=@popup Name="popup"></RadzenCheckBox>
        <RadzenLabel Text="Display validators as popup" Component="popup" />
    </RadzenStack>

    <RadzenTemplateForm TItem="Model" Data=@model Submit=@OnSubmit InvalidSubmit=@OnInvalidSubmit>
        <RadzenFieldset Text="Password">
            <RadzenStack Gap="2rem" class="rz-p-4 rz-p-md-12">
                <RadzenRow AlignItems="AlignItems.Center" RowGap="0.25rem">
                    <RadzenColumn Size="12" SizeMD="4" class="rz-text-align-start rz-text-align-md-end">
                        <RadzenLabel Text="Password" Component="Password" />
                    </RadzenColumn>
                    <RadzenColumn Size="12" SizeMD="8">
                        <RadzenPassword Name="Password" @bind-Value=@model.Password Style="display: block; width: 100%" />
                        <RadzenRequiredValidator Component="Password" Text="Enter password" Popup="@popup" Style="position: absolute" />
                    </RadzenColumn>
                </RadzenRow>
                <RadzenRow AlignItems="AlignItems.Center" RowGap="0.25rem">
                    <RadzenColumn Size="12" SizeMD="4" class="rz-text-align-start rz-text-align-md-end">
                        <RadzenLabel Text="Repeat Password" Component="RepeatPassword" />
                    </RadzenColumn>
                    <RadzenColumn Size="12" SizeMD="8">
                        <RadzenPassword Name="RepeatPassword" @bind-Value=@model.RepeatPassword Style="display: block; width: 100%" />
                        <RadzenRequiredValidator Component="RepeatPassword" Text="Repeat your password" Popup=@popup Style="position: absolute" />
                        <RadzenCompareValidator Visible=@(!string.IsNullOrEmpty(model.RepeatPassword)) Value=@model.Password Component="RepeatPassword" Text="Passwords should be the same" Popup=@popup Style="position: absolute" />
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
        public string Password { get; set; }
        public string RepeatPassword { get; set; }
    }

    bool popup;

    Model model = new Model();
    EventConsole console;

    void Log(string eventName, string value)
    {
        console.Log($"{eventName}: {value}");
    }

    void OnSubmit(Model model)
    {
        Log("Submit", JsonSerializer.Serialize(model, new JsonSerializerOptions() { WriteIndented = true }));
    }

    void OnInvalidSubmit(FormInvalidSubmitEventArgs args)
    {
        Log("InvalidSubmit", JsonSerializer.Serialize(args, new JsonSerializerOptions() { WriteIndented = true }));
    }
}
```


### Conditional Validation

To make the validator conditional you can set its `Visible` property. When set to `false` the validator will not run. In the example above `Visible` is set to `!string.IsNullOrEmpty(model.RepeatPassword)` - the validator will not run if RepeatPassword is empty.

### Comparison operator

By default RadzenCompareValidator checks if the component value is equal to `Value`. This can be changed via the `Operator` property.

```razor
<RadzenStack class="rz-p-0 rz-p-md-12">
    <RadzenTemplateForm TItem="Model" Data=@model Submit=@OnSubmit InvalidSubmit=@OnInvalidSubmit>
        <RadzenFieldset Text="Comparison operator">
            <RadzenStack Gap="2rem" class="rz-p-4 rz-p-md-12">
                <RadzenRow AlignItems="AlignItems.Center" RowGap="0.25rem">
                    <RadzenColumn Size="12" SizeMD="4" class="rz-text-align-start rz-text-align-md-end">
                        <RadzenLabel Text="Operator" Component="Operator" />
                    </RadzenColumn>
                    <RadzenColumn Size="12" SizeMD="8">
                        <RadzenDropDown Name="Operator" @bind-Value=@compareOperator Data=@(Enum.GetValues(typeof(CompareOperator)).Cast<CompareOperator>()) Style="width: 100%" />
                    </RadzenColumn>
                </RadzenRow>
                <RadzenRow AlignItems="AlignItems.Center" RowGap="0.25rem">
                    <RadzenColumn Size="12" SizeMD="4" class="rz-text-align-start rz-text-align-md-end">
                        <RadzenLabel Text="Value to compare with" Component="targetValue" />
                    </RadzenColumn>
                    <RadzenColumn Size="12" SizeMD="8">
                        <RadzenNumeric @bind-Value=@targetValue Style="width: 100%" Name="targetValue" />
                    </RadzenColumn>
                </RadzenRow>
                <RadzenRow AlignItems="AlignItems.Center" RowGap="0.25rem">
                    <RadzenColumn Size="12" SizeMD="4" class="rz-text-align-start rz-text-align-md-end">
                        <RadzenLabel Text="Value" Component="Value" />
                    </RadzenColumn>
                    <RadzenColumn Size="12" SizeMD="8">
                        <RadzenNumeric @bind-Value=@model.Value Name="Value" style="display: block; width: 100%;" />
                        <RadzenCompareValidator Value=@targetValue Component="Value" Style="position: absolute" Operator=@compareOperator />
                    </RadzenColumn>
                </RadzenRow>
                <RadzenRow AlignItems="AlignItems.Center" class="rz-mt-4">
                    <RadzenColumn Size="12" Offset="0" SizeMD="8" OffsetMD="4">
                        <RadzenButton ButtonType="ButtonType.Submit" Text="Validate"></RadzenButton>
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
        public double Value { get; set; }
    }

    double targetValue = 1;

    CompareOperator compareOperator = CompareOperator.Equal;

    Model model = new Model();
    EventConsole console;

    void Log(string eventName, string value)
    {
        console.Log($"{eventName}: {value}");
    }

    void OnSubmit(Model model)
    {
        Log("Submit", JsonSerializer.Serialize(model, new JsonSerializerOptions() { WriteIndented = true }));
    }

    void OnInvalidSubmit(FormInvalidSubmitEventArgs args)
    {
        Log("InvalidSubmit", JsonSerializer.Serialize(args, new JsonSerializerOptions() { WriteIndented = true }));
    }
}
```


### Appearance

By default RadzenCompareValidator appears next to the component it validates. To make it appear below add `Style="display:block"`.
To make it appear as a styled popup set its `Popup` property to `true` and set its CSS `position` to `absolute`. The validated component should have `display: block` so the validation message appears right below it.
