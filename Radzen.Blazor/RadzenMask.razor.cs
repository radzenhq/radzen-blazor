using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public partial class RadzenMask : FormComponent<string>
    {
        [Parameter]
        public bool ReadOnly { get; set; }

        [Parameter]
        public bool AutoComplete { get; set; } = true;

        [Parameter]
        public long? MaxLength { get; set; }

        [Parameter]
        public string Mask { get; set; }

        [Parameter]
        public string Pattern { get; set; }

        protected async System.Threading.Tasks.Task OnChange(ChangeEventArgs args)
        {
            Value = $"{args.Value}";

            await ValueChanged.InvokeAsync(Value);
            if (FieldIdentifier.FieldName != null) { EditContext?.NotifyFieldChanged(FieldIdentifier); }
            await Change.InvokeAsync(Value);
        }

        protected override string GetComponentCssClass()
        {
            return GetClassList("rz-textbox").ToString();
        }
    }
}