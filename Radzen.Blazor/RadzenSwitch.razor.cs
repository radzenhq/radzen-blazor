using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenSwitch component.
    /// </summary>
    /// <example>
    /// <code>
    /// &lt;RadzenSwitch @bind-Value=@value Change=@(args => Console.WriteLine($"Value: {args}")) /&gt;
    /// </code>
    /// </example>
    public partial class RadzenSwitch : FormComponent<bool>
    {
        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-switch").Add("rz-switch-checked", Value).ToString();
        }

        /// <summary>
        /// Specifies additional custom attributes that will be rendered by the input.
        /// </summary>
        /// <value>The attributes.</value>
        [Parameter]
        public IReadOnlyDictionary<string, object> InputAttributes { get; set; }

        private string ValueAsString => Value.ToString().ToLower();

        /// <summary>
        /// Toggles this instance checked state.
        /// </summary>
        public async System.Threading.Tasks.Task Toggle()
        {
            if (Disabled)
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
