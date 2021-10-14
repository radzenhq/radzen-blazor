using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenEmailValidator component.
    /// Implements the <see cref="Radzen.Blazor.ValidatorBase" />
    /// </summary>
    /// <seealso cref="Radzen.Blazor.ValidatorBase" />
    public class RadzenEmailValidator : ValidatorBase
    {
        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        [Parameter]
        public override string Text { get; set; } = "Invalid email";

        /// <summary>
        /// Validates the specified component.
        /// </summary>
        /// <param name="component">The component.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        protected override bool Validate(IRadzenFormComponent component)
        {
            var value = component.GetValue();
            var valueAsString = value as string;

            if (string.IsNullOrEmpty(valueAsString))
            {
                return true;
            }

            var email = new EmailAddressAttribute();

            return email.IsValid(valueAsString);
        }
    }
}