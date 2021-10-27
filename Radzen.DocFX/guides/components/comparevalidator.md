# CompareValidator component
This article demonstrates how to use RadzenCompareValidator.

## Basic usage
RadzenCompareValidator compares the user input agains a predefined value or another component.

To use it perform these steps:
1. Add an input component and set its `Name`. Data-bind its value to a model property via `@bind-Value=@model.Property`.
1. Add RadzenCompareValidator and set its `Component` property to the `Name` of the input component. Set its `Value` property to 
the value you want to compare with (usually another model property).

> [!IMPORTANT]
> RadzenCompareValidator works only inside [RadzenTemplateForm](templateform.md). 

Here is a typical user registration form which checks if the user entered the same password.
```
<RadzenTemplateForm TItem="Registration" Data=@model>
    <p>
      <RadzenLabel Text="Password" />
      <RadzenPassword Name="Password" @bind-Value=@model.Password />
      <RadzenRequiredValidator Component="Password" Text="Enter password" />
    </p>
    <p>
        <RadzenLabel Text="Repeat Password" />
        <RadzenPassword Name="RepeatPassword" @bind-Value=@model.RepeatPassword />
        <RadzenRequiredValidator Component="RepeatPassword" Text="Repeat your password" />
        <RadzenCompareValidator Visible=@(!string.IsNullOrEmpty(model.RepeatPassword)) Value=@model.Password Component="RepeatPassword" Text="Passwords should be the same" Popup=@popup Style="position: absolute" />
    </p>
    <RadzenButton ButtonType="ButtonType.Submit" Text="Submit"></RadzenButton>
</RadzenTemplateForm>
@code {
    class Registration
    {
        public string Password { get; set; }
        public string RepeatPassword { get; set; }
    }

    Registration model = new Registration();
}
```
## Conditional validation
To make the validator conditional you can set its `Visible` property. When set to `false` the validator will not run.
In this example `Visible` is set to `!string.IsNullOrEmpty(model.RepeatPassword)` - the validator will not run if `RepeatPassword` is empty.
```
<RadzenCompareValidator Visible=@(!string.IsNullOrEmpty(model.RepeatPassword)) Value=@model.Password Component="RepeatPassword" Text="Passwords should be the same" Popup=@popup Style="position: absolute" />
```
## Comparison operator
By default RadzenCompareValidator checks if the component value is equal to `Value`. This can be changed via the `Operator` property.
```
<RadzenNumeric Name="Count" @bind-Value=@model.Count />
<RadzenCompareValidator Component="Count" Text="Count should be less than 10" Operator="CompareOperator.LessThan" Value="10" />
```
## Appearance
By default RadzenCompareValidator appears next to the component it validates.

To make it appear below set its `Style` to `"display:block"`. 
```
<RadzenNumeric Name="Count" @bind-Value=@model.Count />
<RadzenCompareValidator Style="display:block" Component="Count" Text="Count should be less than 10" Operator="CompareOperator.LessThan" Value="10" />
```
To make it appear as a styled popup set its `Popup` property to `true` and set its CSS position to `absolute`. The validated component should have `display: block` so the validation message appears right below it.
```
<RadzenNumeric Name="Count" @bind-Value=@model.Count Style="display:block" />
<RadzenCompareValidator Style="position:absolute" Popup="true" Component="Count" Text="Count should be less than 10" Operator="CompareOperator.LessThan" Value="10" />
```