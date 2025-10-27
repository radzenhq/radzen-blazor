using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;

namespace Radzen.Blazor
{
    /// <summary>
    /// A validator component that ensures a text input matches a specified regular expression pattern.
    /// RadzenRegexValidator is useful for validating formats like ZIP codes, phone numbers, custom IDs, or any pattern-based data.
    /// Must be placed inside a <see cref="RadzenTemplateForm{TItem}"/>.
    /// Checks input against a regular expression pattern using .NET's RegularExpressionAttribute.
    /// Common use cases include ZIP/postal codes (e.g., "\d{5}" for US ZIP), phone numbers (e.g., "\d{3}-\d{3}-\d{4}"), custom ID formats (e.g., "^[A-Z]{2}\d{6}$"), and alphanumeric constraints (e.g., "^[a-zA-Z0-9]+$").
    /// The validator passes when the value matches the entire pattern (implicit anchoring). Empty or null values are considered valid - combine with RadzenRequiredValidator to ensure the field is filled.
    /// </summary>
    /// <example>
    /// ZIP code validation:
    /// <code>
    /// &lt;RadzenTemplateForm TItem="Model" Data=@model&gt;
    ///     &lt;RadzenTextBox style="display: block" Name="ZIP" @bind-Value=@model.Zip /&gt;
    ///     &lt;RadzenRegexValidator Component="ZIP" Pattern="^\d{5}$" Text="ZIP code must be 5 digits" Style="position: absolute" /&gt;
    /// &lt;/RadzenTemplateForm&gt;
    /// @code {
    ///     class Model { public string Zip { get; set; } }
    ///     Model model = new Model();
    /// }
    /// </code>
    /// Phone number validation:
    /// <code>
    /// &lt;RadzenTextBox Name="Phone" @bind-Value=@model.Phone /&gt;
    /// &lt;RadzenRegexValidator Component="Phone" Pattern="^\d{3}-\d{3}-\d{4}$" Text="Format: 123-456-7890" /&gt;
    /// </code>
    /// </example>
    public class RadzenRegexValidator : ValidatorBase
    {
        /// <summary>
        /// Gets or sets the error message displayed when the component value does not match the pattern.
        /// Customize this to provide clear guidance on the expected format.
        /// </summary>
        /// <value>The validation error message. Default is "Value should match".</value>
        [Parameter]
        public override string Text { get; set; } = "Value should match";

        /// <summary>
        /// Gets or sets the regular expression pattern that the component value must match.
        /// Use standard .NET regex syntax. The pattern should match the entire value (implicit full-string match).
        /// Example patterns: "^\d{5}$" (5 digits), "^[A-Z]{3}\d{4}$" (3 letters + 4 digits).
        /// </summary>
        /// <value>The regex pattern string.</value>
        [Parameter]
        public string Pattern { get; set; }

        /// <inheritdoc />
        protected override bool Validate(IRadzenFormComponent component)
        {
            return new RegularExpressionAttribute(Pattern).IsValid(component.GetValue());
        }
    }
}