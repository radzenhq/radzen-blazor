using Microsoft.AspNetCore.Components;
using System.ComponentModel.DataAnnotations;

namespace Radzen.Blazor
{
    public class RadzenRegexValidator : ValidatorBase
    {
        [Parameter]
        public override string Text { get; set; } = "Value should match";

        [Parameter]
        public string Pattern { get; set; }

        protected override bool Validate(IRadzenFormComponent component)
        {
            return new RegularExpressionAttribute(Pattern).IsValid(component.GetValue());
        }
    }
}