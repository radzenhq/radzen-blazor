using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// A single-line text input component that supports data binding, validation, and various input behaviors.
    /// RadzenTextBox provides a styled text input with support for placeholders, autocomplete, immediate updates, and string trimming.
    /// Supports two-way data binding via @bind-Value and form validation when used within Radzen forms.
    /// Can be configured for immediate value updates as the user types or deferred updates on blur/change.
    /// Use <see cref="Trim"/> to automatically remove whitespace, and <see cref="MaxLength"/> to limit input length.
    /// </summary>
    /// <example>
    /// Basic usage with two-way binding:
    /// <code>
    /// &lt;RadzenTextBox @bind-Value=@username Placeholder="Enter username" /&gt;
    /// </code>
    /// With immediate updates and trimming:
    /// <code>
    /// &lt;RadzenTextBox @bind-Value=@value Immediate="true" Trim="true" MaxLength="50" /&gt;
    /// </code>
    /// Read-only text box:
    /// <code>
    /// &lt;RadzenTextBox Value=@displayValue ReadOnly="true" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenTextBox : FormComponentWithAutoComplete<string>
    {
        /// <summary>
        /// Gets or sets whether the text box is read-only and cannot be edited by the user.
        /// When true, the text box displays the value but prevents user input while still allowing selection and copying.
        /// </summary>
        /// <value><c>true</c> if the text box is read-only; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of characters that can be entered in the text box.
        /// When set, the browser will prevent users from typing beyond this limit.
        /// </summary>
        /// <value>The maximum character length, or null for no limit. Default is null.</value>
        [Parameter]
        public long? MaxLength { get; set; }

        /// <summary>
        /// Gets or sets whether to automatically remove leading and trailing whitespace from the value.
        /// When enabled, whitespace is trimmed when the value changes.
        /// </summary>
        /// <value><c>true</c> to trim whitespace; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool Trim { get; set; }

        /// <summary>
        /// Gets or sets whether the component should update the bound value immediately as the user types (oninput event),
        /// rather than waiting for the input to lose focus (onchange event).
        /// This enables real-time value updates but may trigger more frequent change events.
        /// </summary>
        /// <value><c>true</c> for immediate updates; <c>false</c> for deferred updates. Default is <c>false</c>.</value>
        [Parameter]
        public bool Immediate { get; set; }

        /// <summary>
        /// Handles the change event of the underlying HTML input element.
        /// Applies trimming if enabled and notifies the edit context and change listeners.
        /// </summary>
        /// <param name="args">The change event arguments containing the new value.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected async Task OnChange(ChangeEventArgs args)
        {
            Value = $"{args.Value}";

            if (Trim)
            {
                Value = Value.Trim();
            }

            await ValueChanged.InvokeAsync(Value);

            if (FieldIdentifier.FieldName != null)
            {
                EditContext?.NotifyFieldChanged(FieldIdentifier);
            }

            await Change.InvokeAsync(Value);
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-textbox").ToString();
        }

        /// <inheritdoc />
        protected override string GetId()
        {
            return Name ?? base.GetId();
        }
    }
}
