# RegexValidator component
This article demonstrates how to use RadzenRegexValidator.

## Basic usage
RadzenRegexValidator checks if the user input matches the specified regular expression.

To use it perform these steps:
1. Add an input component and set its `Name`. Data-bind its value to a model property via `@bind-Value=@model.Zip`.
1. Add RadzenRegexValidator and set its `Component` property to the `Name` of the input component. Set the `Pattern` property to the regular expression you want to validate against.

> [!IMPORTANT]
> RadzenRegexValidator works only inside [RadzenTemplateForm](templateform.md). 

```
<RadzenTemplateForm TItem="Registration" Data=@model>
    <p>
       <RadzenTextBox Name="ZIP" @bind-Value=@model.Zip />
       <RadzenRegexValidator Component="ZIP" Text="ZIP code must be 5 digits" Pattern="\d{5}" />
    </p>
    <RadzenButton ButtonType="ButtonType.Submit" Text="Submit"></RadzenButton>
</RadzenTemplateForm>
@code {
    class Model
    {
        public string Zip { get; set; }
    }

    Model model = new Model();
}
```
## Conditional validation
To make the validator conditional you can set its `Visible` property. When set to `false` the validator will not run.
## Appearance
By default RadzenRegexValidator appears next to the component it validates.

To make it appear below set its `Style` to `"display:block"`. 
```
<RadzenTextBox Name="ZIP" @bind-Value=@model.Zip />
<RadzenRegexValidator Style="display:block" Component="ZIP" Text="ZIP code must be 5 digits" Pattern="\d{5}" />
```
To make it appear as a styled popup set its `Popup` property to `true` and set its CSS position to `absolute`. The validated component should have `display: block` so the validation message appears right below it.
```
<RadzenTextBox Style="display:block" Name="ZIP" @bind-Value=@model.Zip />
<RadzenRegexValidator Popup="true" Style="position:absolute" Component="ZIP" Text="ZIP code must be 5 digits" Pattern="\d{5}" />
```