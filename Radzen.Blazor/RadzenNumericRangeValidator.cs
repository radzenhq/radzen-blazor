using System;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public class RadzenNumericRangeValidator : ValidatorBase
    {
        [Parameter]
        public override string Text { get; set; } = "Not in the valid range";

        [Parameter]
        public dynamic Min { get; set; }

        [Parameter]
        public dynamic Max { get; set; }
        protected override bool Validate(IRadzenFormComponent component)
        {
            dynamic value = component.GetValue();

            if (Min != null && ((value != null && value < Min) || value == null))
            {
                return false;
            }

            if (Max != null && (value != null && value > Max))
            {
                return false;
            }

            return true;
        }
    }
}