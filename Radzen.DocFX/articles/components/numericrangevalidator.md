# NumericRangeValidator component
This article demonstrates how to use RadzenNumericRangeValidator.

## Basic usage
RadzenNumericRangeValidator checks if the user enters a number within a specified range.

To use it perform these steps:
1. Add an input component and set its `Name`. Data-bind its value to a model property via `@bind-Value=@model.Quantity`.
1. Add RadzenNumericRangeValidator and set its `Component` property to the `Name` of the input component. Set `Min`, `Max` or both
to specify the valid range.

> [!IMPORTANT]
> RadzenNumericRangeValidator works only inside [RadzenTemplateForm](templateform.md). 

```
<RadzenTemplateForm TItem="Registration" Data=@model>
    <p>
        <RadzenNumeric Name="Quantity" @bind-Value=@model.Quantity />
        <RadzenNumericRangeValidator Component="Quantity" Min="1" Max="10" Text="Quantity should be between 1 and 10" />
    </p>
    <RadzenButton ButtonType="ButtonType.Submit" Text="Submit"></RadzenButton>
</RadzenTemplateForm>
@code {
    class Model
    {
        public decimal Quantity { get; set; }
    }

    Model model = new Model();
}
```
## Conditional validation
To make the validator conditional you can set its `Visible` property. When set to `false` the validator will not run.
## Appearance
By default RadzenNumericRangeValidator appears next to the component it validates.

To make it appear below set its `Style` to `"display:block"`. 
```
<RadzenNumeric Name="Quantity" @bind-Value=@model.Quantity />
<RadzenNumericRangeValidator Style="display:block" Component="Quantity" Min="1" Max="10" Text="Quantity should be between 1 and 10" />
```
To make it appear as a styled popup set its `Popup` property to `true` and set its CSS position to `absolute`. The validated component should have `display: block` so the validation message appears right below it.
```
<RadzenNumeric Name="Quantity" Style="display:block" @bind-Value=@model.Quantity />
<RadzenNumericRangeValidator Popup="true" Style="position:absolute" Component="Quantity" Min="1" Max="10" Text="Quantity should be between 1 and 10" />
```