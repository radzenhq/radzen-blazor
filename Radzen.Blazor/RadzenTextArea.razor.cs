using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public partial class RadzenTextArea : FormComponent<string>
    {
        [Parameter]
        public long? MaxLength { get; set; }

        [Parameter]
        public bool ReadOnly { get; set; }

        [Parameter]
        public int Rows { get; set; } = 2;

        [Parameter]
        public int Cols { get; set; } = 20;

        protected async System.Threading.Tasks.Task OnChange(ChangeEventArgs args)
        {
            Value = $"{args.Value}";

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);
        }

        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-textarea").ToString();
        }
    }
}