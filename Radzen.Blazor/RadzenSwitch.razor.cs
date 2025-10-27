using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// A toggle switch component for boolean on/off states with a sliding animation.
    /// RadzenSwitch provides an alternative to checkboxes with a more modern toggle UI pattern, ideal for settings and preferences.
    /// Displays as a sliding toggle that users can click or drag to change between on (true) and off (false) states, providing visual feedback with a sliding animation and color change.
    /// Common uses include enabling/disabling settings or features, toggling visibility of sections, on/off preferences in configuration panels, and boolean options in forms.
    /// Supports keyboard navigation (Space/Enter to toggle) for accessibility. Unlike checkboxes, switches are typically used for immediate effects rather than form submission actions.
    /// </summary>
    /// <example>
    /// Basic switch:
    /// <code>
    /// &lt;RadzenSwitch @bind-Value=@isEnabled /&gt;
    /// </code>
    /// Switch with label and change event:
    /// <code>
    /// &lt;RadzenStack Orientation="Orientation.Horizontal" Gap="0.5rem" AlignItems="AlignItems.Center"&gt;
    ///     &lt;RadzenSwitch @bind-Value=@notifications Change=@(enabled => Console.WriteLine($"Notifications: {enabled}")) /&gt;
    ///     &lt;RadzenLabel Text="Enable notifications" /&gt;
    /// &lt;/RadzenStack&gt;
    /// </code>
    /// Read-only switch for display:
    /// <code>
    /// &lt;RadzenSwitch Value=@feature.IsEnabled ReadOnly="true" /&gt;
    /// </code>
    /// </example>
    public partial class RadzenSwitch : FormComponent<bool>
    {
        /// <summary>
        /// Gets or sets whether the switch is read-only and cannot be toggled by user interaction.
        /// When true, the switch displays its current state but prevents clicking or keyboard toggling.
        /// Useful for displaying switch state in view-only forms or dashboards.
        /// </summary>
        /// <value><c>true</c> if the switch is read-only; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        [Parameter]
        public bool ReadOnly { get; set; }

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-switch").Add("rz-switch-checked", Value).ToString();
        }

        /// <summary>
        /// Gets or sets additional HTML attributes to be applied to the underlying input element.
        /// This allows passing custom attributes like data-* attributes, aria-* attributes, or other HTML attributes directly to the input.
        /// </summary>
        /// <value>A dictionary of custom HTML attributes.</value>
        [Parameter]
        public IReadOnlyDictionary<string, object> InputAttributes { get; set; }

        private string ValueAsString => Value.ToString().ToLower();

        /// <summary>
        /// Programmatically toggles the switch between on (true) and off (false) states.
        /// This method is public and can be called from code to toggle the switch without user interaction.
        /// Does nothing if the switch is disabled or read-only.
        /// </summary>
        /// <returns>A task representing the asynchronous toggle operation.</returns>
        public async System.Threading.Tasks.Task Toggle()
        {
            if (Disabled || ReadOnly)
            {
                return;
            }

            Value = !Value;

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);
        }

        bool preventKeyPress = true;
        async Task OnKeyPress(KeyboardEventArgs args, Task task)
        {
            var key = args.Code != null ? args.Code : args.Key;

            if (key == "Space" || key == "Enter")
            {
                preventKeyPress = true;

                await task;
            }
            else
            {
                preventKeyPress = false;
            }
        }
    }
}
