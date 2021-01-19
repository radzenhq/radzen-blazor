using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Radzen.Blazor
{
    public class RadzenRequiredValidator : ValidatorBase
    {
        [Parameter]
        public override string Text { get; set; } = "Required";

        protected override bool Validate(IRadzenFormComponent component)
        {
            return component.HasValue && !object.Equals(DefaultValue, component.GetValue());
        }

        [Parameter]
        public object DefaultValue { get; set; }
    }
}