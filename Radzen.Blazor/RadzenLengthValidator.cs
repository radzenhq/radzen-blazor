using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public class RadzenLengthValidator : ValidatorBase
    {
        [Parameter]
        public override string Text { get; set; } = "Invalid length";

        [Parameter]
        public int? Min { get; set; }

        [Parameter]
        public int? Max { get; set; }

        protected override bool Validate(IRadzenFormComponent component)
        {
            string value = component.GetValue() as string;

            if (Min.HasValue && ((value != null && value.Length < Min) || value == null))
            {
                return false;
            }

            if (Max.HasValue && (value != null && value.Length > Max))
            {
                return false;
            }

            return true;
        }
    }
}