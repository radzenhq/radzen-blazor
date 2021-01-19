using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    public class RadzenEmailValidator : ValidatorBase
    {
        [Parameter]
        public override string Text { get; set; } = "Invalid email";

        protected override bool Validate(IRadzenFormComponent component)
        {
            var email = new EmailAddressAttribute();

            return email.IsValid(component.GetValue());
        }
    }
}