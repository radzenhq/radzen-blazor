using System;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A validator component that executes custom validation logic via a user-provided function.
    /// RadzenCustomValidator enables complex validation rules that cannot be achieved with built-in validators, such as database checks, cross-field validation, or business rule enforcement.
    /// Must be placed inside a <see cref="RadzenTemplateForm{TItem}"/>.
    /// Provides complete flexibility for validation logic by executing a Func&lt;bool&gt; that you define. The validator is valid when the function returns true, invalid when it returns false.
    /// Common use cases include uniqueness checks (validating email/username against existing database records), business rules (enforcing domain-specific validation logic),
    /// cross-field validation (validating relationships between multiple fields), API validation (checking values against external services), and any complex logic requiring custom code.
    /// The Validator function should return true for valid values and false for invalid values. The function is called during form validation, so keep it fast or use async patterns for slow operations.
    /// </summary>
    /// <example>
    /// Uniqueness validation:
    /// <code>
    /// &lt;RadzenTemplateForm TItem="Model" Data=@model&gt;
    ///     &lt;RadzenTextBox Name="Email" @bind-Value=@model.Email /&gt;
    ///     &lt;RadzenCustomValidator Component="Email" Text="Email already exists" 
    ///                            Validator=@(() => !existingEmails.Contains(model.Email)) 
    ///                            Style="position: absolute" /&gt;
    /// &lt;/RadzenTemplateForm&gt;
    /// @code {
    ///     class Model { public string Email { get; set; } }
    ///     Model model = new Model();
    ///     string[] existingEmails = new[] { "user@example.com", "admin@example.com" };
    /// }
    /// </code>
    /// Date range validation:
    /// <code>
    /// &lt;RadzenDatePicker Name="StartDate" @bind-Value=@model.StartDate /&gt;
    /// &lt;RadzenDatePicker Name="EndDate" @bind-Value=@model.EndDate /&gt;
    /// &lt;RadzenCustomValidator Component="EndDate" Validator=@(() => model.EndDate > model.StartDate) 
    ///                        Text="End date must be after start date" /&gt;
    /// </code>
    /// </example>
    public class RadzenCustomValidator : ValidatorBase
    {
        /// <summary>
        /// Gets or sets the error message displayed when the validation function returns false.
        /// Provide clear, actionable text explaining why the value is invalid and how to fix it.
        /// </summary>
        /// <value>The validation error message. Default is "Value should match".</value>
        [Parameter]
        public override string Text { get; set; } = "Value should match";

        /// <summary>
        /// Gets or sets the validation function that determines whether the component value is valid.
        /// The function should return true if the value is valid, false if invalid.
        /// This function is called during form validation, so keep it fast or handle async operations appropriately.
        /// </summary>
        /// <value>The validation function. Default returns true (always valid).</value>
        [Parameter]
        public Func<bool> Validator { get; set; } = () => true;

        /// <inheritdoc />
        protected override bool Validate(IRadzenFormComponent component)
        {
            return Validator();
        }
    }
}