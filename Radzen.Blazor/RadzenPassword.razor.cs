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
            // Do NOT assign to the local Value field here. The @bind:get/:set machinery
            // relies on the bound .NET value remaining at the parent's value when the
            // parent rejects (or does not update) the input; in that case, the framework's
            // post-handler sync writes the bound value back to the DOM. If we mutated
            // _value to match the user input, _value and the user-typed DOM would agree
            // and the renderer would have nothing to sync. Let ValueChanged propagate;
            // the parent's two-way binding (or lack of it) determines what Value becomes
            // on the next parameter assignment.
            var newValue = $"{value}";

            await ValueChanged.InvokeAsync(newValue);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
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
