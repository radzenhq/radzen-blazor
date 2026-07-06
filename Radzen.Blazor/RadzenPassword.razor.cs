using Microsoft.AspNetCore.Components;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// A password input component that masks entered characters for secure password entry with autocomplete support.
    /// RadzenPassword provides a styled password field with browser autocomplete integration for password managers.
    /// Displays entered characters as dots or asterisks to protect sensitive data from shoulder surfing, integrates with browser password managers by setting appropriate autocomplete attributes.
    /// Supports data binding, validation, placeholder text, and read-only mode for display purposes.
    /// Use within forms for login, registration, password change, or any scenario requiring secure text entry.
    /// </summary>
    /// <example>
    /// Basic password input:
    /// <code>
    /// &lt;RadzenPassword @bind-Value=@password Placeholder="Enter password" /&gt;
    /// </code>
    /// Password confirmation with validation:
    /// <code>
    /// &lt;RadzenTemplateForm Data=@model&gt;
    ///     &lt;RadzenPassword Name="Password" @bind-Value=@model.Password Placeholder="Password" /&gt;
    ///     &lt;RadzenRequiredValidator Component="Password" Text="Password is required" /&gt;
    ///     &lt;RadzenPassword Name="ConfirmPassword" @bind-Value=@model.ConfirmPassword Placeholder="Confirm password" /&gt;
    ///     &lt;RadzenCompareValidator Value=@model.Password Component="ConfirmPassword" Text="Passwords must match" /&gt;
    /// &lt;/RadzenTemplateForm&gt;
    /// </code>
    /// </example>
    public partial class RadzenPassword : FormComponentWithAutoComplete<string>, IRadzenFormComponent
    {
        /// <summary>
        /// Gets or sets whether the password input is read-only and cannot be edited.
        /// When true, displays the masked value (or placeholder) but prevents user input.
        /// Useful for displaying password field in view-only forms, though typically passwords are not displayed at all.
        /// </summary>
        /// <value><c>true</c> if the input is read-only; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets whether the component should update the bound value immediately as the user types (oninput event),
        /// rather than waiting for the input to lose focus (onchange event).
        /// This enables real-time value updates but may trigger more frequent change events.
        /// </summary>
        /// <value><c>true</c> for immediate updates; <c>false</c> for deferred updates. Default is <c>false</c>.</value>
        [Parameter]
        public bool Immediate { get; set; }

        /// <summary>
        /// Handles the @bind:set binding of the underlying input element.
        /// </summary>
        /// <param name="value">The new value reported by the input/change event.</param>
        protected async System.Threading.Tasks.Task SetValue(string? value)
        {
            // When ValueChanged is wired, leave _value alone — parameter re-flow handles
            // both accepted updates (Value setter overwrites _value) and parent rejection
            // (parameter unchanged → Blazor skips SetParametersAsync → _value stays at the
            // bound value → @bind:get force-syncs the DOM back to it). When ValueChanged
            // is NOT wired, no re-flow occurs, so _value must be updated locally or
            // @bind:get would re-evaluate to the stale initial value and the framework
            // would wipe the DOM on every blur.
            var newValue = $"{value}";
            if (!IsBound)
            {
                Value = newValue;
            }

            await ValueChanged.InvokeAsync(newValue);
            NotifyFieldChanged(newValue);
            await Change.InvokeAsync(newValue);
        }

        /// <summary>
        /// Gets or sets the size of the component.
        /// </summary>
        [Parameter]
        public InputSize InputSize { get; set; } = InputSize.Medium;

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-textbox").AddInputSize(InputSize).ToString();
        }

        /// <inheritdoc />
        public override string DefaultAutoCompleteAttribute { get; set; } = "new-password";

        /// <inheritdoc />
        protected override string? GetId()
        {
            return Name ?? base.GetId();
        }
    }
}
