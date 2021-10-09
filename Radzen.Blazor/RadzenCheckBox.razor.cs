using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenCheckBox.
    /// Implements the <see cref="Radzen.FormComponent{TValue}" />
    /// </summary>
    /// <typeparam name="TValue">The type of the t value.</typeparam>
    /// <seealso cref="Radzen.FormComponent{TValue}" />
    public partial class RadzenCheckBox<TValue> : FormComponent<TValue>
    {
        /// <summary>
        /// Gets or sets a value indicating whether [tri state].
        /// </summary>
        /// <value><c>true</c> if [tri state]; otherwise, <c>false</c>.</value>
        [Parameter]
        public bool TriState { get; set; } = false;

        /// <summary>
        /// Gets the box class list.
        /// </summary>
        /// <value>The box class list.</value>
        ClassList BoxClassList => ClassList.Create("rz-chkbox-box")
                                           .Add("rz-state-active", !object.Equals(Value, false))
                                           .AddDisabled(Disabled);

        /// <summary>
        /// Gets the icon class list.
        /// </summary>
        /// <value>The icon class list.</value>
        ClassList IconClassList => ClassList.Create("rz-chkbox-icon")
                                            .Add("rzi rzi-check", object.Equals(Value, true))
                                            .Add("rzi rzi-times", object.Equals(Value, null));

        /// <summary>
        /// Gets the component CSS class.
        /// </summary>
        /// <returns>System.String.</returns>
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-chkbox").ToString();
        }

        /// <summary>
        /// Handles the <see cref="E:KeyPress" /> event.
        /// </summary>
        /// <param name="args">The <see cref="KeyboardEventArgs"/> instance containing the event data.</param>
        async Task OnKeyPress(KeyboardEventArgs args)
        {
            if (args.Code == "Space")
            {
                await Toggle();
            }
        }

        /// <summary>
        /// Toggles this instance.
        /// </summary>
        async Task Toggle()
        {
            if (Disabled)
            {
                return;
            }

            if (object.Equals(Value, false))
            {
                if (TriState)
                {
                    Value = default(TValue);
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