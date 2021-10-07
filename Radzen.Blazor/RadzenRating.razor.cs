using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public partial class RadzenRating : FormComponent<int>
    {
        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-rating").Add("rz-state-readonly", ReadOnly).ToString();
        }

        [Parameter]
        public int Stars { get; set; } = 5;

        [Parameter]
        public bool ReadOnly { get; set; }

        private async System.Threading.Tasks.Task SetValue(int value)
        {
            if (!Disabled && !ReadOnly)
            {
                Value = value;

                await ValueChanged.InvokeAsync(value);
                if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
                await Change.InvokeAsync(value);
            }
        }
    }
}