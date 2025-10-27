using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    /// <summary>
    /// A validator component that ensures a form input component has a non-empty value.
    /// RadzenRequiredValidator verifies that required fields are filled before form submission.
    /// Must be placed inside a <see cref="RadzenTemplateForm{TItem}"/> and associated with a named input component.
    /// Checks if the associated component has a value (HasValue returns true) and that the value is not equal to the optional DefaultValue.
    /// For text inputs, an empty string is considered invalid. For dropdowns and other components, null or default values are considered invalid.
    /// The validation message can be customized via the Text property and displayed inline, as a block, or as a popup depending on the Style property.
    /// </summary>
    /// <example>
    /// Basic required field validation:
    /// <code>
    /// &lt;RadzenTemplateForm TItem="Model" Data=@model&gt;
    ///   &lt;RadzenTextBox style="display: block" Name="Email" @bind-Value=@model.Email /&gt;
    ///   &lt;RadzenRequiredValidator Component="Email" Text="Email is required" Style="position: absolute" /&gt;
    /// &lt;/RadzenTemplateForm&gt;
    /// @code {
    ///  class Model
    ///  {
    ///    public string Email { get; set; }
    ///  }
    ///  
    ///  Model model = new Model();
    /// }
    /// </code>
    /// Dropdown validation with default value:
    /// <code>
    /// &lt;RadzenDropDown Name="Country" @bind-Value=@model.CountryId Data=@countries /&gt;
    /// &lt;RadzenRequiredValidator Component="Country" DefaultValue="0" Text="Please select a country" /&gt;
    /// </code>
    /// </example>
    public class RadzenRequiredValidator : ValidatorBase
    {
        /// <summary>
        /// Gets or sets the error message displayed when the associated component is invalid (has no value or has the default value).
        /// This message helps users understand what is required to pass validation.
        /// </summary>
        /// <value>The validation error message. Default is "Required".</value>
        [Parameter]
        public override string Text { get; set; } = "Required";

        /// <inheritdoc />
        protected override bool Validate(IRadzenFormComponent component)
        {
            return component.HasValue && !object.Equals(DefaultValue, component.GetValue());
        }

        /// <summary>
        /// Gets or sets a default value that should be considered invalid (empty).
        /// For example, set to 0 for numeric dropdowns where 0 represents "not selected", or empty Guid for Guid fields.
        /// If the component's value equals this DefaultValue, validation will fail.
        /// </summary>
        /// <value>The value to treat as empty/invalid. Default is null.</value>
        [Parameter]
        public object DefaultValue { get; set; }
    }
}