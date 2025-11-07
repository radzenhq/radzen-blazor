using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A checkbox input component that supports two-state (checked/unchecked) or tri-state (checked/unchecked/indeterminate) modes.
    /// RadzenCheckBox provides data binding, validation, and keyboard accessibility for boolean or nullable boolean values.
    /// In two-state mode, the value toggles between true and false. In tri-state mode (<see cref="TriState"/> = true), the value cycles through false → null → true → false.
    /// Supports keyboard interaction (Space/Enter to toggle) and integrates with Blazor EditContext for form validation.
    /// </summary>
    /// <typeparam name="TValue">The type of the bound value. Typically bool for two-state or bool? for tri-state checkboxes.</typeparam>
    /// <example>
    /// Basic two-state checkbox:
    /// <code>
    /// &lt;RadzenCheckBox @bind-Value=@isChecked TValue="bool" /&gt;
    /// </code>
    /// Tri-state checkbox with change handler:
    /// <code>
    /// &lt;RadzenCheckBox @bind-Value=@nullableBool TValue="bool?" TriState="true" Change=@(args => Console.WriteLine($"Value: {args}")) /&gt;
    /// </code>
    /// Read-only checkbox for display purposes:
    /// <code>
    /// &lt;RadzenCheckBox Value=@isEnabled TValue="bool" ReadOnly="true" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenCheckBox<TValue> : FormComponent<TValue>
    {
        /// <summary>
        /// Gets or sets additional HTML attributes to be applied to the underlying input element.
        /// This allows passing custom attributes like data-* attributes, aria-* attributes, or other HTML attributes directly to the input.
        /// </summary>
        /// <value>A dictionary of custom HTML attributes.</value>
        [Parameter]
        public IReadOnlyDictionary<string, object> InputAttributes { get; set; }

        /// <summary>
        /// Gets or sets whether the checkbox supports three states: checked (true), unchecked (false), and indeterminate (null).
        /// When enabled, clicking cycles through all three states. Use with nullable boolean (<c>bool?</c>) values.
        /// </summary>
        /// <value><c>true</c> to enable tri-state mode; <c>false</c> for standard two-state mode. Default is <c>false</c>.</value>
        [Parameter]
        public bool TriState { get; set; } = false;

        string BoxClass => ClassList.Create("rz-chkbox-box")
                                    .Add("rz-state-active", !object.Equals(Value, false))
                                    .AddDisabled(Disabled)
                                    .ToString();

        string IconClass => ClassList.Create("notranslate rz-chkbox-icon")
                                     .Add("rzi rzi-check", object.Equals(Value, true))
                                     .Add("rzi rzi-times", object.Equals(Value, null))
                                     .ToString();

        string CheckBoxValue => CheckBoxChecked ? "true" : "false";

        bool CheckBoxChecked => object.Equals(Value, true);

        /// <inheritdoc />
        protected override string GetComponentCssClass() => GetClassList("rz-chkbox").ToString();

        async Task OnKeyPress(KeyboardEventArgs args)
        {
            if (args.Code == "Space")
            {
                await Toggle();
            }
        }

        /// <summary>
        /// Gets or sets whether the checkbox is read-only and cannot be toggled by user interaction.
        /// When true, the checkbox displays its current state but prevents clicking or keyboard toggling.
        /// Useful for displaying checkbox state in view-only scenarios.
        /// </summary>
        /// <value><c>true</c> if the checkbox is read-only; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        async Task Toggle()
        {
            if (Disabled || ReadOnly)
            {
                return;
            }

            if (object.Equals(Value, false))
            {
                if (TriState)
                {
                    Value = default;
                }
                else
                {
                    Value = (TValue)(object)true;
                }
            }
            else if (Value == null)
            {
                Value = (TValue)(object)true;
            }
            else if (object.Equals(Value, true))
            {
                Value = (TValue)(object)false;
            }

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);
        }
    }
}
