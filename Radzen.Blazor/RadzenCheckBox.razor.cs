using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenCheckBox component.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <example>
    /// <code>
    /// &lt;RadzenCheckBox @bind-Value=@someValue TValue="bool" Change=@(args => Console.WriteLine($"Is checked: {args}")) /&gt;
    /// </code>
    /// </example>
    public partial class RadzenCheckBox<TValue> : FormComponent<TValue>
    {
        /// <summary>
        /// Gets or sets a value indicating whether is tri-state (true, false or null).
        /// </summary>
        /// <value><c>true</c> if tri-state; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool TriState { get; set; } = false;

        ClassList BoxClassList => ClassList.Create("rz-chkbox-box")
                                           .Add("rz-state-active", !object.Equals(Value, false))
                                           .AddDisabled(Disabled);

        ClassList IconClassList => ClassList.Create("rz-chkbox-icon")
                                            .Add("rzi rzi-check", object.Equals(Value, true))
                                            .Add("rzi rzi-times", object.Equals(Value, null));

        string CheckBoxValue => CheckBoxChecked ? "true" : "false";

        bool CheckBoxChecked => object.Equals(Value, true);

        /// <inheritdoc />
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-chkbox").ToString();
        }

        async Task OnKeyPress(KeyboardEventArgs args)
        {
            if (args.Code == "Space")
            {
                await Toggle();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether is read only.
        /// </summary>
        /// <value><c>true</c> if is read only; otherwise, <c>false</c>.</value>
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
