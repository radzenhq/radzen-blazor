# LengthValidator component
This article demonstrates how to use RadzenLengthValidator.

## Basic usage
RadzenLengthValidator checks if the user input is within specified length.

To use it perform these steps:
1. Add an input component and set its `Name`. Data-bind its value to a model property via `@bind-Value=@model.FirstName`.
1. Add RadzenLengthValidator and set its `Component` property to the `Name` of the input component. Set `Min`, `Max` or both
to specify the valid string length.

> [!IMPORTANT]
> RadzenLengthValidator works only inside [RadzenTemplateForm](templateform.md). 

```
<RadzenTemplateForm TItem="Registration" Data=@model>
    <p>
      <RadzenTextBox style="display: block" Name="FirstName" @bind-Value=@model.FirstName />
      <RadzenLengthValidator Component="FirstName" Min="3" Text="First name should be at least 3 characters" />
      <RadzenLengthValidator Component="FirstName" Max="10" Text="First name should be at most 10 characters" />
    </p>
    <RadzenButton ButtonType="ButtonType.Submit" Text="Submit"></RadzenButton>
</RadzenTemplateForm>
@code {
    class Registration
    {
        public string FirstName { get; set; }
    }

    Registration model = new Registration();
}
```
## Conditional validation
To make the validator conditional you can set its `Visible` property. When set to `false` the validator will not run.
## Appearance
By default RadzenLengthValidator appears next to the component it validates.

To make it appear below set its `Style` to `"display:block"`. 
```
<RadzenTextBox Name="FirstName" @bind-Value=@model.FirstName />
<RadzenLengthValidator Style="display:block" Component="FirstName" Min="3" Text="First name should be at least 3 characters" />
```
To make it appear as a styled popup set its `Popup` property to `true` and set its CSS position to `absolute`. The validated component should have `display: block` so the validation message appears right below it.
```
<RadzenTextBox Name="FirstName" @bind-Value=@model.FirstName Style="display:block" />
<RadzenLengthValidator Popup="true" Style="position:absolute" Component="FirstName" Min="3" Text="First name should be at least 3 characters" />
```