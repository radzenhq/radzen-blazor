using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Radzen.Blazor.Rendering;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    public partial class RadzenCheckBox<TValue> : FormComponent<TValue>
    {
        [Parameter]
        public bool TriState { get; set; } = false;

        ClassList BoxClassList => ClassList.Create("rz-chkbox-box")
                                           .Add("rz-state-active", !object.Equals(Value, false))
                                           .AddDisabled(Disabled);

        ClassList IconClassList => ClassList.Create("rz-chkbox-icon")
                                            .Add("rzi rzi-check", object.Equals(Value, true))
                                            .Add("rzi rzi-times", object.Equals(Value, null));

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