using Microsoft.AspNetCore.Components.Web;
using System.Threading.Tasks;

namespace Radzen.Blazor
{
    public partial class RadzenSwitch : FormComponent<bool>
    {
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-switch").Add("rz-switch-checked", Value).ToString();
        }

        public async Task OnMouseUp(MouseEventArgs args)
        {
            await Toggle();
        }

        async System.Threading.Tasks.Task Toggle()
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
    }
}